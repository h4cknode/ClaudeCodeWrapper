using System.Text.Json;
using ClaudeCodeWrapper.Models;
using ClaudeCodeWrapper.Models.Records;

namespace ClaudeCodeWrapper.Core;

/// <summary>
/// Repository for loading complete Claude Code sessions with all related files.
/// </summary>
public class SessionRepository
{
    private readonly string _claudeDir;

    /// <summary>
    /// Creates a new session repository.
    /// </summary>
    /// <param name="claudeDir">Path to ~/.claude directory. Defaults to user's home.</param>
    public SessionRepository(string? claudeDir = null)
    {
        _claudeDir = claudeDir ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".claude");
    }

    /// <summary>
    /// Path to the ~/.claude directory.
    /// </summary>
    public string ClaudeDirectory => _claudeDir;

    /// <summary>
    /// Load a complete session with all related data.
    /// </summary>
    public async Task<Session?> LoadSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        // Find session file
        var sessionFile = FindSessionFile(sessionId);
        if (sessionFile == null) return null;

        var projectDir = Path.GetDirectoryName(sessionFile)!;

        // Load main session records
        var records = await SessionRecordParser.ParseFileAsync(sessionFile, cancellationToken);

        // Extract metadata from first record
        var firstRecord = records.FirstOrDefault();
        var slug = firstRecord?.Slug;
        var cwd = firstRecord?.Cwd;
        var gitBranch = firstRecord?.GitBranch;
        var version = firstRecord?.Version;

        // Load agent sessions
        var agents = await LoadAgentSessionsAsync(projectDir, sessionId, cancellationToken);

        // Load todos
        var todos = await LoadTodosAsync(sessionId, cancellationToken);

        // Load file history
        var fileHistory = LoadFileHistory(sessionId);

        // Find debug log
        var debugLogPath = GetDebugLogPath(sessionId);

        // Calculate timestamps
        var timestamps = records
            .Where(r => r.Timestamp.HasValue)
            .Select(r => r.Timestamp!.Value)
            .ToList();

        return new Session
        {
            Id = sessionId,
            Slug = slug,
            SessionFilePath = sessionFile,
            ProjectPath = projectDir,
            Cwd = cwd,
            GitBranch = gitBranch,
            Version = version,
            StartedAt = timestamps.Count > 0 ? timestamps.Min() : null,
            LastActivityAt = timestamps.Count > 0 ? timestamps.Max() : null,
            Records = records,
            Agents = agents,
            Todos = todos,
            FileHistory = fileHistory,
            DebugLogPath = File.Exists(debugLogPath) ? debugLogPath : null
        };
    }

    /// <summary>
    /// List all sessions in a project directory.
    /// </summary>
    public IEnumerable<SessionInfo> ListSessions(string? projectPath = null)
    {
        var projectsDir = Path.Combine(_claudeDir, "projects");
        if (!Directory.Exists(projectsDir))
            yield break;

        IEnumerable<string> directories;
        if (projectPath != null)
        {
            var encodedPath = EncodeProjectPath(projectPath);
            var dir = Path.Combine(projectsDir, encodedPath);
            directories = Directory.Exists(dir) ? [dir] : [];
        }
        else
        {
            directories = Directory.GetDirectories(projectsDir);
        }

        foreach (var dir in directories)
        {
            var sessionFiles = Directory.GetFiles(dir, "*.jsonl")
                .Where(f => !Path.GetFileName(f).StartsWith("agent-"));

            foreach (var file in sessionFiles)
            {
                var sessionId = Path.GetFileNameWithoutExtension(file);
                var fileInfo = new FileInfo(file);

                yield return new SessionInfo
                {
                    SessionId = sessionId,
                    FilePath = file,
                    ProjectPath = dir,
                    CreatedAt = fileInfo.CreationTimeUtc,
                    ModifiedAt = fileInfo.LastWriteTimeUtc,
                    SizeBytes = fileInfo.Length
                };
            }
        }
    }

    /// <summary>
    /// Find session file by ID.
    /// </summary>
    public string? FindSessionFile(string sessionId)
    {
        var projectsDir = Path.Combine(_claudeDir, "projects");
        if (!Directory.Exists(projectsDir))
            return null;

        var files = Directory.GetFiles(projectsDir, $"{sessionId}.jsonl", SearchOption.AllDirectories);
        return files.FirstOrDefault();
    }

    /// <summary>
    /// Load agent sessions for a parent session.
    /// </summary>
    private async Task<IReadOnlyList<AgentSession>> LoadAgentSessionsAsync(
        string projectDir,
        string parentSessionId,
        CancellationToken cancellationToken)
    {
        var agents = new List<AgentSession>();
        var agentFiles = Directory.GetFiles(projectDir, "agent-*.jsonl");

        foreach (var agentFile in agentFiles)
        {
            var records = await SessionRecordParser.ParseFileAsync(agentFile, cancellationToken);

            // Check if this agent belongs to the parent session
            var firstRecord = records.FirstOrDefault();
            if (firstRecord?.SessionId != parentSessionId)
                continue;

            var agentId = Path.GetFileNameWithoutExtension(agentFile).Replace("agent-", "");

            agents.Add(new AgentSession
            {
                AgentId = agentId,
                FilePath = agentFile,
                ParentSessionId = parentSessionId,
                Records = records
            });
        }

        return agents;
    }

    /// <summary>
    /// Load todos for a session.
    /// </summary>
    private async Task<IReadOnlyList<TodoItem>> LoadTodosAsync(
        string sessionId,
        CancellationToken cancellationToken)
    {
        var todosDir = Path.Combine(_claudeDir, "todos");
        if (!Directory.Exists(todosDir))
            return [];

        // Look for todo files matching this session
        var todoFiles = Directory.GetFiles(todosDir, $"{sessionId}-*.json");
        if (todoFiles.Length == 0)
            return [];

        // Use the most recent one
        var latestTodoFile = todoFiles
            .OrderByDescending(f => new FileInfo(f).LastWriteTimeUtc)
            .First();

        try
        {
            var json = await File.ReadAllTextAsync(latestTodoFile, cancellationToken);
            var todos = JsonSerializer.Deserialize<List<TodoItemDto>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return todos?.Select(t => new TodoItem
            {
                Content = t.Content ?? "",
                Status = t.Status ?? "pending",
                ActiveForm = t.ActiveForm ?? ""
            }).ToList() ?? [];
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Load file history entries for a session.
    /// </summary>
    private IReadOnlyList<FileHistoryEntry> LoadFileHistory(string sessionId)
    {
        var historyDir = Path.Combine(_claudeDir, "file-history", sessionId);
        if (!Directory.Exists(historyDir))
            return [];

        var entries = new List<FileHistoryEntry>();
        foreach (var file in Directory.GetFiles(historyDir))
        {
            var fileName = Path.GetFileName(file);
            var fileInfo = new FileInfo(file);

            // Parse version from filename (format: hash@vN)
            var version = 1;
            var atIndex = fileName.LastIndexOf("@v", StringComparison.Ordinal);
            if (atIndex > 0 && int.TryParse(fileName[(atIndex + 2)..], out var v))
                version = v;

            entries.Add(new FileHistoryEntry
            {
                OriginalPath = "", // Would need snapshot data to know original path
                BackupPath = file,
                Version = version,
                BackupTime = fileInfo.LastWriteTimeUtc,
                Size = fileInfo.Length
            });
        }

        return entries;
    }

    /// <summary>
    /// Get debug log path for a session.
    /// </summary>
    private string GetDebugLogPath(string sessionId)
    {
        return Path.Combine(_claudeDir, "debug", $"{sessionId}.txt");
    }

    /// <summary>
    /// Read debug log content.
    /// </summary>
    public async Task<string?> ReadDebugLogAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        var path = GetDebugLogPath(sessionId);
        if (!File.Exists(path))
            return null;

        return await File.ReadAllTextAsync(path, cancellationToken);
    }

    /// <summary>
    /// Encode a project path to Claude's format.
    /// </summary>
    public static string EncodeProjectPath(string path)
    {
        // Replace forward slashes with hyphens, handle leading slash
        var encoded = path.Replace("/", "-");
        if (!encoded.StartsWith("-"))
            encoded = "-" + encoded;
        return encoded;
    }

    /// <summary>
    /// Decode a project path from Claude's format.
    /// </summary>
    public static string DecodeProjectPath(string encoded)
    {
        return encoded.Replace("-", "/");
    }

    // DTO for JSON deserialization
    private class TodoItemDto
    {
        public string? Content { get; set; }
        public string? Status { get; set; }
        public string? ActiveForm { get; set; }
    }
}

/// <summary>
/// Basic session information (without loading full content).
/// </summary>
public record SessionInfo
{
    /// <summary>
    /// Session UUID.
    /// </summary>
    public required string SessionId { get; init; }

    /// <summary>
    /// Path to session .jsonl file.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Project directory path.
    /// </summary>
    public required string ProjectPath { get; init; }

    /// <summary>
    /// When the session file was created.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// When the session file was last modified.
    /// </summary>
    public DateTime ModifiedAt { get; init; }

    /// <summary>
    /// Size of the session file in bytes.
    /// </summary>
    public long SizeBytes { get; init; }
}
