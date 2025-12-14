namespace ClaudeCodeWrapper.Models.Records;

/// <summary>
/// Queue operation record - tracks message queue management.
/// </summary>
public record QueueOperationRecord : SessionRecord
{
    /// <summary>
    /// Operation type: "enqueue" or "dequeue".
    /// </summary>
    public required string Operation { get; init; }

    /// <summary>
    /// Content associated with the operation.
    /// </summary>
    public string? Content { get; init; }
}
