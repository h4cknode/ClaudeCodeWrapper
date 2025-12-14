using ClaudeCodeWrapper.Models.Blocks;

namespace ClaudeCodeWrapper.Models.Records;

/// <summary>
/// Assistant response record - includes AI output, tool calls, and usage.
/// </summary>
public record AssistantRecord : SessionRecord
{
    /// <summary>
    /// Full API response message.
    /// </summary>
    public required AssistantMessage Message { get; init; }

    /// <summary>
    /// API request ID for this response.
    /// </summary>
    public string? RequestId { get; init; }

    /// <summary>
    /// Whether this message represents an API error.
    /// </summary>
    public bool IsApiErrorMessage { get; init; }

    /// <summary>
    /// Error type if this is an error message.
    /// </summary>
    public string? Error { get; init; }
}

/// <summary>
/// Assistant message from Claude API.
/// </summary>
public record AssistantMessage
{
    /// <summary>
    /// Model used (e.g., "claude-opus-4-5-20251101").
    /// </summary>
    public required string Model { get; init; }

    /// <summary>
    /// API message ID.
    /// </summary>
    public string? Id { get; init; }

    /// <summary>
    /// Message type (always "message").
    /// </summary>
    public string MessageType { get; init; } = "message";

    /// <summary>
    /// Role (always "assistant").
    /// </summary>
    public string Role { get; init; } = "assistant";

    /// <summary>
    /// Content blocks (text, thinking, tool_use).
    /// </summary>
    public required IReadOnlyList<ContentBlock> Content { get; init; }

    /// <summary>
    /// Stop reason: "end_turn", "tool_use", or null.
    /// </summary>
    public string? StopReason { get; init; }

    /// <summary>
    /// Stop sequence if applicable.
    /// </summary>
    public string? StopSequence { get; init; }

    /// <summary>
    /// Token usage for this response.
    /// </summary>
    public TokenUsage? Usage { get; init; }

    /// <summary>
    /// Context management info.
    /// </summary>
    public ContextManagement? ContextManagement { get; init; }

    /// <summary>
    /// Get all text blocks.
    /// </summary>
    public IEnumerable<TextBlock> TextBlocks => Content.OfType<TextBlock>();

    /// <summary>
    /// Get all thinking blocks.
    /// </summary>
    public IEnumerable<ThinkingBlock> ThinkingBlocks => Content.OfType<ThinkingBlock>();

    /// <summary>
    /// Get all tool use blocks.
    /// </summary>
    public IEnumerable<ToolUseBlock> ToolUseBlocks => Content.OfType<ToolUseBlock>();

    /// <summary>
    /// Combined text output.
    /// </summary>
    public string FullText => string.Join("\n", TextBlocks.Select(t => t.Text));

    /// <summary>
    /// Whether this message contains tool calls.
    /// </summary>
    public bool HasToolCalls => Content.Any(c => c is ToolUseBlock);
}

/// <summary>
/// Context management information.
/// </summary>
public record ContextManagement
{
    /// <summary>
    /// Whether context was truncated.
    /// </summary>
    public bool Truncated { get; init; }

    /// <summary>
    /// Reason for truncation.
    /// </summary>
    public string? Reason { get; init; }
}
