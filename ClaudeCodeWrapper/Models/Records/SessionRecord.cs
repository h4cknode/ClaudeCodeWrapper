namespace ClaudeCodeWrapper.Models.Records;

/// <summary>
/// Base record for all Claude Code session log entries.
/// Each line in a .jsonl file is one of these record types.
/// </summary>
public abstract record SessionRecord
{
    /// <summary>
    /// Record type: "user", "assistant", "summary", "system", "queue-operation", "file-history-snapshot"
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// ISO 8601 timestamp when this record was created.
    /// </summary>
    public DateTime? Timestamp { get; init; }

    /// <summary>
    /// Session UUID this record belongs to.
    /// </summary>
    public string? SessionId { get; init; }

    /// <summary>
    /// Unique identifier for this message/record.
    /// </summary>
    public string? Uuid { get; init; }

    /// <summary>
    /// Parent message UUID for conversation threading.
    /// Null for root messages.
    /// </summary>
    public string? ParentUuid { get; init; }

    /// <summary>
    /// Current working directory when this record was created.
    /// </summary>
    public string? Cwd { get; init; }

    /// <summary>
    /// Git branch active when this record was created.
    /// </summary>
    public string? GitBranch { get; init; }

    /// <summary>
    /// Claude Code version (e.g., "2.0.65").
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// User type: "external" (human) or "internal" (system).
    /// </summary>
    public string? UserType { get; init; }

    /// <summary>
    /// Whether this is from a sub-agent (sidechain).
    /// </summary>
    public bool IsSidechain { get; init; }

    /// <summary>
    /// Agent ID for sub-agent records (7-character ID).
    /// </summary>
    public string? AgentId { get; init; }

    /// <summary>
    /// Human-readable session name (e.g., "sunny-frolicking-penguin").
    /// </summary>
    public string? Slug { get; init; }
}
