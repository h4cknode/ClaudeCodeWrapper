using System.Collections.Concurrent;
using System.Reactive.Subjects;
using ClaudeCodeWrapper.Models;
using ClaudeCodeWrapper.Models.Records;

namespace ClaudeCodeWrapper.Core;

/// <summary>
/// Monitors Claude session logs and emits records as they occur.
/// Implements IObservable for flexible event consumption using Reactive Extensions.
///
/// Usage:
/// <code>
/// var monitor = new SessionMonitor(new SessionMonitorOptions { WorkingDirectory = "/path/to/project" });
/// monitor.Subscribe(record => Console.WriteLine($"{record.Type}: {record.Uuid}"));
/// monitor.Start();
/// // ... execute Claude commands ...
/// monitor.Stop();
/// monitor.Dispose();
/// </code>
/// </summary>
public class SessionMonitor : IObservable<SessionRecord>, IDisposable
{
    private readonly SessionMonitorOptions _options;
    private readonly Subject<SessionRecord> _subject = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _fileLocks = new();

    private FileSystemWatcher? _parentDirectoryWatcher;
    private FileSystemWatcher? _projectDirectoryWatcher;
    private FileSystemWatcher? _sessionFileWatcher;
    private Timer? _pollingTimer;

    private string? _sessionFilePath;
    private string? _projectDirectory;
    private string? _currentSessionId;
    private long _lastPosition;
    private DateTime _watchStartTime;
    private bool _isMonitoring;
    private bool _disposed;

    // Track multiple files (main session + agent files)
    private readonly Dictionary<string, long> _filePositions = new();
    private readonly HashSet<string> _trackedFiles = new();

    /// <summary>
    /// Polling interval in milliseconds (FileSystemWatcher can be unreliable on macOS).
    /// </summary>
    private const int PollingIntervalMs = 100;

    /// <summary>
    /// Creates a new session monitor with the specified options.
    /// </summary>
    public SessionMonitor(SessionMonitorOptions? options = null)
    {
        _options = options ?? new SessionMonitorOptions();
    }

    /// <summary>
    /// Current session ID being monitored (if known).
    /// </summary>
    public string? CurrentSessionId => _currentSessionId;

    /// <summary>
    /// Whether monitoring is currently active.
    /// </summary>
    public bool IsMonitoring => _isMonitoring;

    /// <summary>
    /// Path to the current session file.
    /// </summary>
    public string? SessionFilePath => _sessionFilePath;

    /// <summary>
    /// All tracked files (main session + agents).
    /// </summary>
    public IReadOnlyCollection<string> TrackedFiles => _trackedFiles;

    /// <summary>
    /// Raised when an error occurs during monitoring.
    /// </summary>
    public event EventHandler<Exception>? Error;

    /// <summary>
    /// Subscribe to session records.
    /// </summary>
    public IDisposable Subscribe(IObserver<SessionRecord> observer)
    {
        return _subject.Subscribe(observer);
    }

    /// <summary>
    /// Start monitoring for session activities.
    /// Call this BEFORE starting Claude execution.
    /// </summary>
    public void Start()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SessionMonitor));

        if (_isMonitoring)
            return;

        _isMonitoring = true;
        _watchStartTime = DateTime.UtcNow;
        _lastPosition = 0;
        _sessionFilePath = null;

        try
        {
            // If session ID is provided, monitor that specific session
            if (!string.IsNullOrEmpty(_options.SessionId))
            {
                StartWatchingSession(_options.SessionId);
                return;
            }

            // Otherwise, watch for new session files in the project directory
            var claudeProjectsDir = _options.GetClaudeProjectsPath();

            if (!Directory.Exists(claudeProjectsDir))
            {
                return;
            }

            _projectDirectory = _options.GetDerivedProjectPath();

            if (string.IsNullOrEmpty(_projectDirectory))
            {
                return;
            }

            if (Directory.Exists(_projectDirectory))
            {
                StartWatchingProjectDirectory();
                ScanForSessionFiles();
            }
            else
            {
                WatchForProjectDirectoryCreation(claudeProjectsDir);
            }

            // Start fallback polling timer - FileSystemWatcher can be unreliable on macOS
            _pollingTimer = new Timer(OnPollingTimerCallback, null, 0, PollingIntervalMs);
        }
        catch (Exception ex)
        {
            OnError(ex);
        }
    }

    /// <summary>
    /// Stop monitoring. Can be restarted with Start().
    /// </summary>
    public void Stop()
    {
        _isMonitoring = false;
        CleanupWatchers();
    }

    /// <summary>
    /// Manually trigger reading of new lines from all tracked files.
    /// </summary>
    public async Task ReadNewRecordsAsync(CancellationToken cancellationToken = default)
    {
        var tasks = _trackedFiles.ToArray()
            .Select(f => ReadNewLinesFromFileAsync(f, cancellationToken));
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Get the full session once monitoring is complete.
    /// </summary>
    public async Task<Session?> GetSessionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentSessionId == null) return null;

        var repository = new SessionRepository(Path.GetDirectoryName(_options.GetClaudeProjectsPath()));
        return await repository.LoadSessionAsync(_currentSessionId, cancellationToken);
    }

    private void ScanForSessionFiles()
    {
        if (_projectDirectory == null || !Directory.Exists(_projectDirectory)) return;

        try
        {
            var jsonlFiles = Directory.GetFiles(_projectDirectory, "*.jsonl");
            var toleranceTime = _watchStartTime.AddSeconds(-_options.NewFileToleranceSeconds);

            foreach (var file in jsonlFiles)
            {
                try
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTimeUtc >= toleranceTime)
                    {
                        TrackFile(file);
                    }
                }
                catch
                {
                    // Ignore file access errors
                }
            }
        }
        catch
        {
            // Ignore scan errors
        }
    }

    private void TrackFile(string file)
    {
        if (_trackedFiles.Add(file))
        {
            _filePositions[file] = 0;

            var fileName = Path.GetFileNameWithoutExtension(file);
            if (!fileName.StartsWith("agent-") && _sessionFilePath == null)
            {
                _currentSessionId = fileName;
                _sessionFilePath = file;
                _lastPosition = 0;
            }
        }
    }

    private void OnPollingTimerCallback(object? state)
    {
        if (_disposed || !_isMonitoring) return;

        try
        {
            if (_projectDirectory != null && !Directory.Exists(_projectDirectory))
            {
                return;
            }

            ScanForSessionFiles();

            var tasks = _trackedFiles.ToArray()
                .Select(f => ReadNewLinesFromFileAsync(f, CancellationToken.None));
            Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch
        {
            // Ignore polling errors
        }
    }

    private void StartWatchingSession(string sessionId)
    {
        var claudeProjectsDir = _options.GetClaudeProjectsPath();

        if (!Directory.Exists(claudeProjectsDir))
            return;

        var sessionFiles = Directory.GetFiles(claudeProjectsDir, $"{sessionId}.jsonl", SearchOption.AllDirectories);
        _sessionFilePath = sessionFiles.FirstOrDefault();

        if (_sessionFilePath == null)
            return;

        _currentSessionId = sessionId;

        if (_options.IncludeExistingContent)
        {
            _ = ReadNewLinesFromFileAsync(_sessionFilePath, CancellationToken.None);
        }

        var directory = Path.GetDirectoryName(_sessionFilePath)!;
        var fileName = Path.GetFileName(_sessionFilePath);

        _sessionFileWatcher = new FileSystemWatcher(directory, fileName)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
        };

        _sessionFileWatcher.Changed += OnSessionFileChanged;
        _sessionFileWatcher.EnableRaisingEvents = true;
    }

    private void WatchForProjectDirectoryCreation(string parentDir)
    {
        if (_projectDirectory == null) return;

        var targetSubdir = Path.GetFileName(_projectDirectory);

        _parentDirectoryWatcher = new FileSystemWatcher(parentDir)
        {
            NotifyFilter = NotifyFilters.DirectoryName,
            IncludeSubdirectories = false
        };

        _parentDirectoryWatcher.Created += (s, e) =>
        {
            if (e.Name == targetSubdir)
            {
                _projectDirectory = e.FullPath;
                StartWatchingProjectDirectory();
            }
        };

        _parentDirectoryWatcher.EnableRaisingEvents = true;
    }

    private void StartWatchingProjectDirectory()
    {
        if (_projectDirectory == null || _disposed || !_isMonitoring) return;

        _projectDirectoryWatcher = new FileSystemWatcher(_projectDirectory, "*.jsonl")
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName
        };

        _projectDirectoryWatcher.Created += OnNewSessionFileCreated;
        _projectDirectoryWatcher.Changed += OnSessionFileChanged;
        _projectDirectoryWatcher.EnableRaisingEvents = true;
    }

    private void OnNewSessionFileCreated(object sender, FileSystemEventArgs e)
    {
        if (_disposed || !_isMonitoring) return;

        try
        {
            var fileInfo = new FileInfo(e.FullPath);
            var toleranceTime = _watchStartTime.AddSeconds(-_options.NewFileToleranceSeconds);

            if (fileInfo.CreationTimeUtc < toleranceTime)
                return;

            TrackFile(e.FullPath);
            _ = ReadNewLinesFromFileAsync(e.FullPath, CancellationToken.None);
        }
        catch (Exception ex)
        {
            OnError(ex);
        }
    }

    private void OnSessionFileChanged(object sender, FileSystemEventArgs e)
    {
        if (_disposed || !_isMonitoring) return;

        if (!_trackedFiles.Contains(e.FullPath))
        {
            try
            {
                var fileInfo = new FileInfo(e.FullPath);
                var toleranceTime = _watchStartTime.AddSeconds(-_options.NewFileToleranceSeconds);

                if (fileInfo.CreationTimeUtc < toleranceTime)
                    return;

                TrackFile(e.FullPath);
            }
            catch
            {
                return;
            }
        }

        _ = ReadNewLinesFromFileAsync(e.FullPath, CancellationToken.None);
    }

    private async Task ReadNewLinesFromFileAsync(string filePath, CancellationToken cancellationToken)
    {
        if (_disposed || !_isMonitoring || !File.Exists(filePath)) return;

        var fileLock = _fileLocks.GetOrAdd(filePath, _ => new SemaphoreSlim(1, 1));

        if (!await fileLock.WaitAsync(0, cancellationToken))
            return;

        try
        {
            if (!_filePositions.TryGetValue(filePath, out var lastPosition))
            {
                lastPosition = 0;
                _filePositions[filePath] = 0;
            }

            await using var fs = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite,
                bufferSize: 4096,
                useAsync: true);

            if (fs.Length <= lastPosition)
                return;

            fs.Seek(lastPosition, SeekOrigin.Begin);
            using var reader = new StreamReader(fs, bufferSize: 4096);

            string? line;
            while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var record = SessionRecordParser.Parse(line);
                if (record != null)
                {
                    _subject.OnNext(record);
                }
            }

            _filePositions[filePath] = fs.Position;

            if (filePath == _sessionFilePath)
            {
                _lastPosition = fs.Position;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            OnError(ex);
        }
        finally
        {
            fileLock.Release();
        }
    }

    private void OnError(Exception ex)
    {
        Error?.Invoke(this, ex);
    }

    private void CleanupWatchers()
    {
        _pollingTimer?.Dispose();
        _pollingTimer = null;

        _parentDirectoryWatcher?.Dispose();
        _parentDirectoryWatcher = null;

        _projectDirectoryWatcher?.Dispose();
        _projectDirectoryWatcher = null;

        _sessionFileWatcher?.Dispose();
        _sessionFileWatcher = null;
    }

    /// <summary>
    /// Dispose and release all resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _isMonitoring = false;
        CleanupWatchers();

        foreach (var fileLock in _fileLocks.Values)
            fileLock.Dispose();
        _fileLocks.Clear();

        _subject.OnCompleted();
        _subject.Dispose();
    }
}
