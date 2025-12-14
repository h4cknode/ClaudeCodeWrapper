using System.Text.Json;

namespace ClaudeCodeWrapper.Models.Blocks;

/// <summary>
/// Base for all content blocks in assistant/user messages.
/// </summary>
public abstract record ContentBlock
{
    /// <summary>
    /// Block type: "text", "thinking", "tool_use", "tool_result", "image"
    /// </summary>
    public required string Type { get; init; }
}

/// <summary>
/// Plain text response from the assistant.
/// </summary>
public record TextBlock : ContentBlock
{
    /// <summary>
    /// The text content.
    /// </summary>
    public required string Text { get; init; }
}

/// <summary>
/// Extended thinking/reasoning block.
/// </summary>
public record ThinkingBlock : ContentBlock
{
    /// <summary>
    /// The thinking/reasoning content.
    /// </summary>
    public required string Thinking { get; init; }
}

/// <summary>
/// Tool invocation by the assistant.
/// </summary>
public record ToolUseBlock : ContentBlock
{
    /// <summary>
    /// Unique tool use ID for correlating with results.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Tool name (e.g., "Read", "Write", "Bash", "Grep").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Tool input parameters as raw JSON.
    /// </summary>
    public JsonElement? Input { get; init; }

    /// <summary>
    /// Optional signature for the tool call.
    /// </summary>
    public string? Signature { get; init; }

    /// <summary>
    /// Get input as typed object.
    /// </summary>
    public T? GetInput<T>() where T : class
    {
        if (Input == null) return null;
        return JsonSerializer.Deserialize<T>(Input.Value.GetRawText());
    }
}

/// <summary>
/// Result of a tool execution.
/// </summary>
public record ToolResultBlock : ContentBlock
{
    /// <summary>
    /// Tool use ID this result corresponds to.
    /// </summary>
    public required string ToolUseId { get; init; }

    /// <summary>
    /// Result content (string or complex object).
    /// </summary>
    public string? Content { get; init; }

    /// <summary>
    /// Whether this result represents an error.
    /// </summary>
    public bool IsError { get; init; }
}

/// <summary>
/// Image content (base64 encoded).
/// </summary>
public record ImageBlock : ContentBlock
{
    /// <summary>
    /// Image source information.
    /// </summary>
    public required ImageSource Source { get; init; }
}

/// <summary>
/// Image source with base64 data.
/// </summary>
public record ImageSource
{
    /// <summary>
    /// Source type (usually "base64").
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Media type (e.g., "image/png", "image/jpeg").
    /// </summary>
    public required string MediaType { get; init; }

    /// <summary>
    /// Base64-encoded image data.
    /// </summary>
    public required string Data { get; init; }
}
