using ClaudeCodeWrapper.Models.Blocks;

namespace ClaudeCodeWrapper.Models.Records;

/// <summary>
/// User message record - includes user input and tool results.
/// </summary>
public record UserRecord : SessionRecord
{
    /// <summary>
    /// Message content - either plain text or array of tool results.
    /// </summary>
    public required UserMessage Message { get; init; }

    /// <summary>
    /// Additional tool result metadata (stdout, stderr, etc.).
    /// </summary>
    public ToolUseResultInfo? ToolUseResult { get; init; }

    /// <summary>
    /// Thinking mode metadata.
    /// </summary>
    public ThinkingMetadata? ThinkingMetadata { get; init; }

    /// <summary>
    /// Current todo list at this point.
    /// </summary>
    public IReadOnlyList<TodoItem>? Todos { get; init; }

    /// <summary>
    /// Whether this is a meta message.
    /// </summary>
    public bool IsMeta { get; init; }

    /// <summary>
    /// Whether this is only visible in transcript.
    /// </summary>
    public bool IsVisibleInTranscriptOnly { get; init; }

    /// <summary>
    /// Whether this is a compact summary.
    /// </summary>
    public bool IsCompactSummary { get; init; }
}

/// <summary>
/// User message content.
/// </summary>
public record UserMessage
{
    /// <summary>
    /// Role (always "user").
    /// </summary>
    public string Role { get; init; } = "user";

    /// <summary>
    /// Content as plain string (for user input).
    /// </summary>
    public string? ContentString { get; init; }

    /// <summary>
    /// Content as blocks (for tool results).
    /// </summary>
    public IReadOnlyList<ContentBlock>? ContentBlocks { get; init; }

    /// <summary>
    /// Whether content is tool results (array) vs plain text.
    /// </summary>
    public bool IsToolResults => ContentBlocks != null;
}

/// <summary>
/// Additional metadata for tool execution results.
/// </summary>
public record ToolUseResultInfo
{
    /// <summary>
    /// Standard output from tool execution.
    /// </summary>
    public string? Stdout { get; init; }

    /// <summary>
    /// Standard error from tool execution.
    /// </summary>
    public string? Stderr { get; init; }

    /// <summary>
    /// Whether the tool was interrupted.
    /// </summary>
    public bool Interrupted { get; init; }

    /// <summary>
    /// Whether the result is an image.
    /// </summary>
    public bool IsImage { get; init; }

    /// <summary>
    /// Task status (for Task tool results).
    /// </summary>
    public string? Status { get; init; }

    /// <summary>
    /// Original prompt (for Task tool results).
    /// </summary>
    public string? Prompt { get; init; }
}

/// <summary>
/// Thinking mode metadata.
/// </summary>
public record ThinkingMetadata
{
    /// <summary>
    /// Whether thinking mode is enabled.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// Budget tokens for thinking.
    /// </summary>
    public int? BudgetTokens { get; init; }
}

/// <summary>
/// Todo item from the session.
/// </summary>
public record TodoItem
{
    /// <summary>
    /// Task description.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Status: "pending", "in_progress", "completed"
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Active form (present tense description).
    /// </summary>
    public required string ActiveForm { get; init; }
}
