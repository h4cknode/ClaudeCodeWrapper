using ClaudeCodeWrapper.Models.Blocks;
using ClaudeCodeWrapper.Models.Records;

namespace ClaudeCodeWrapper.Models;

/// <summary>
/// Complete session aggregate - contains all data related to a Claude Code session.
/// This is the main object representing a session with all its relationships.
/// </summary>
public class Session
{
    /// <summary>
    /// Session UUID.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Human-readable session name (e.g., "sunny-frolicking-penguin").
    /// </summary>
    public string? Slug { get; init; }

    /// <summary>
    /// Path to the main session file.
    /// </summary>
    public string? SessionFilePath { get; init; }

    /// <summary>
    /// Project path this session belongs to.
    /// </summary>
    public string? ProjectPath { get; init; }

    /// <summary>
    /// Working directory for this session.
    /// </summary>
    public string? Cwd { get; init; }

    /// <summary>
    /// Git branch when session started.
    /// </summary>
    public string? GitBranch { get; init; }

    /// <summary>
    /// Claude Code version used.
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// When the session started.
    /// </summary>
    public DateTime? StartedAt { get; init; }

    /// <summary>
    /// When the session last had activity.
    /// </summary>
    public DateTime? LastActivityAt { get; init; }

    /// <summary>
    /// All records in the main session file.
    /// </summary>
    public IReadOnlyList<SessionRecord> Records { get; init; } = [];

    /// <summary>
    /// Sub-agent sessions spawned by this session.
    /// </summary>
    public IReadOnlyList<AgentSession> Agents { get; init; } = [];

    /// <summary>
    /// Current todo items.
    /// </summary>
    public IReadOnlyList<TodoItem> Todos { get; init; } = [];

    /// <summary>
    /// File history snapshots for undo functionality.
    /// </summary>
    public IReadOnlyList<FileHistoryEntry> FileHistory { get; init; } = [];

    /// <summary>
    /// Path to debug log file.
    /// </summary>
    public string? DebugLogPath { get; init; }

    #region Computed Properties

    /// <summary>
    /// All user records.
    /// </summary>
    public IEnumerable<UserRecord> UserRecords => Records.OfType<UserRecord>();

    /// <summary>
    /// All assistant records.
    /// </summary>
    public IEnumerable<AssistantRecord> AssistantRecords => Records.OfType<AssistantRecord>();

    /// <summary>
    /// All summary records.
    /// </summary>
    public IEnumerable<SummaryRecord> Summaries => Records.OfType<SummaryRecord>();

    /// <summary>
    /// All system records.
    /// </summary>
    public IEnumerable<SystemRecord> SystemRecords => Records.OfType<SystemRecord>();

    /// <summary>
    /// All file history snapshot records.
    /// </summary>
    public IEnumerable<FileHistorySnapshotRecord> FileHistorySnapshots =>
        Records.OfType<FileHistorySnapshotRecord>();

    /// <summary>
    /// Total number of messages (user + assistant).
    /// </summary>
    public int MessageCount => UserRecords.Count() + AssistantRecords.Count();

    /// <summary>
    /// Total input tokens used.
    /// </summary>
    public long TotalInputTokens => AssistantRecords
        .Sum(a => a.Message.Usage?.InputTokens ?? 0);

    /// <summary>
    /// Total output tokens used.
    /// </summary>
    public long TotalOutputTokens => AssistantRecords
        .Sum(a => a.Message.Usage?.OutputTokens ?? 0);

    /// <summary>
    /// Total tokens (input + output).
    /// </summary>
    public long TotalTokens => TotalInputTokens + TotalOutputTokens;

    /// <summary>
    /// Total cache read tokens (savings).
    /// </summary>
    public long TotalCacheReadTokens => AssistantRecords
        .Sum(a => a.Message.Usage?.CacheReadInputTokens ?? 0);

    /// <summary>
    /// Average cache hit rate across all responses.
    /// </summary>
    public double AverageCacheHitRate
    {
        get
        {
            var usages = AssistantRecords
                .Select(a => a.Message.Usage)
                .Where(u => u != null && u.InputTokens > 0)
                .ToList();
            return usages.Count > 0 ? usages.Average(u => u!.CacheHitRate) : 0;
        }
    }

    /// <summary>
    /// All tool calls made in this session.
    /// </summary>
    public IEnumerable<ToolUseBlock> AllToolCalls => AssistantRecords
        .SelectMany(a => a.Message.ToolUseBlocks);

    /// <summary>
    /// Tool usage counts.
    /// </summary>
    public IReadOnlyDictionary<string, int> ToolUsageCounts => AllToolCalls
        .GroupBy(t => t.Name)
        .ToDictionary(g => g.Key, g => g.Count());

    /// <summary>
    /// Models used in this session with counts.
    /// </summary>
    public IReadOnlyDictionary<string, int> ModelUsage => AssistantRecords
        .GroupBy(a => a.Message.Model)
        .ToDictionary(g => g.Key, g => g.Count());

    /// <summary>
    /// Total web search requests.
    /// </summary>
    public int TotalWebSearchRequests => AssistantRecords
        .Sum(a => a.Message.Usage?.ServerToolUse?.WebSearchRequests ?? 0);

    /// <summary>
    /// Total web fetch requests.
    /// </summary>
    public int TotalWebFetchRequests => AssistantRecords
        .Sum(a => a.Message.Usage?.ServerToolUse?.WebFetchRequests ?? 0);

    /// <summary>
    /// Files modified in this session (from file history).
    /// </summary>
    public IEnumerable<string> ModifiedFiles => FileHistorySnapshots
        .SelectMany(s => s.Snapshot.TrackedFileBackups.Keys)
        .Distinct();

    /// <summary>
    /// Whether this session has any errors.
    /// </summary>
    public bool HasErrors => AssistantRecords.Any(a => a.IsApiErrorMessage);

    /// <summary>
    /// Error records.
    /// </summary>
    public IEnumerable<AssistantRecord> Errors => AssistantRecords
        .Where(a => a.IsApiErrorMessage);

    #endregion

    #region Navigation

    /// <summary>
    /// Get conversation thread starting from a message.
    /// </summary>
    public IEnumerable<SessionRecord> GetThread(string uuid)
    {
        var recordsByUuid = Records.Where(r => r.Uuid != null)
            .ToDictionary(r => r.Uuid!);

        var result = new List<SessionRecord>();
        var current = recordsByUuid.GetValueOrDefault(uuid);

        while (current != null)
        {
            result.Insert(0, current);
            current = current.ParentUuid != null
                ? recordsByUuid.GetValueOrDefault(current.ParentUuid)
                : null;
        }

        return result;
    }

    /// <summary>
    /// Get children of a message.
    /// </summary>
    public IEnumerable<SessionRecord> GetChildren(string parentUuid) =>
        Records.Where(r => r.ParentUuid == parentUuid);

    /// <summary>
    /// Get root messages (no parent).
    /// </summary>
    public IEnumerable<SessionRecord> RootMessages =>
        Records.Where(r => r.ParentUuid == null && r.Uuid != null);

    #endregion
}

/// <summary>
/// Sub-agent session data.
/// </summary>
public class AgentSession
{
    /// <summary>
    /// 7-character agent ID.
    /// </summary>
    public required string AgentId { get; init; }

    /// <summary>
    /// Path to the agent's .jsonl file.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Parent session ID.
    /// </summary>
    public required string ParentSessionId { get; init; }

    /// <summary>
    /// Records from this agent's session.
    /// </summary>
    public IReadOnlyList<SessionRecord> Records { get; init; } = [];

    /// <summary>
    /// Agent type/description (from Task tool).
    /// </summary>
    public string? AgentType { get; init; }

    /// <summary>
    /// Original prompt given to the agent.
    /// </summary>
    public string? Prompt { get; init; }
}

/// <summary>
/// File history entry - represents a backed up file version.
/// </summary>
public record FileHistoryEntry
{
    /// <summary>
    /// Original file path.
    /// </summary>
    public required string OriginalPath { get; init; }

    /// <summary>
    /// Backup file path in ~/.claude/file-history/
    /// </summary>
    public required string BackupPath { get; init; }

    /// <summary>
    /// Version number.
    /// </summary>
    public int Version { get; init; }

    /// <summary>
    /// When the backup was created.
    /// </summary>
    public DateTime BackupTime { get; init; }

    /// <summary>
    /// Size of the backup file.
    /// </summary>
    public long Size { get; init; }
}
