namespace ClaudeCodeWrapper.Models.Records;

/// <summary>
/// System message record - internal system messages and metadata.
/// </summary>
public record SystemRecord : SessionRecord
{
    /// <summary>
    /// System message subtype (e.g., "init", "config").
    /// </summary>
    public required string Subtype { get; init; }

    /// <summary>
    /// Message content.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Log level (e.g., "info", "debug", "error").
    /// </summary>
    public string? Level { get; init; }

    /// <summary>
    /// Logical parent UUID (may differ from parentUuid).
    /// </summary>
    public string? LogicalParentUuid { get; init; }

    /// <summary>
    /// Compact metadata for summarization.
    /// </summary>
    public CompactMetadata? CompactMetadata { get; init; }
}

/// <summary>
/// Metadata for compact/summarized messages.
/// </summary>
public record CompactMetadata
{
    /// <summary>
    /// Number of messages summarized.
    /// </summary>
    public int? MessageCount { get; init; }

    /// <summary>
    /// Total tokens in summarized messages.
    /// </summary>
    public int? TotalTokens { get; init; }
}
