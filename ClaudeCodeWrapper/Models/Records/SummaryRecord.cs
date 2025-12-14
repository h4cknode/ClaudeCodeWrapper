namespace ClaudeCodeWrapper.Models.Records;

/// <summary>
/// Conversation summary record - created for context management.
/// </summary>
public record SummaryRecord : SessionRecord
{
    /// <summary>
    /// Summary text of the conversation.
    /// </summary>
    public required string Summary { get; init; }

    /// <summary>
    /// UUID of the leaf message this summary covers.
    /// </summary>
    public required string LeafUuid { get; init; }
}
