namespace ClaudeCodeWrapper.Models.Records;

/// <summary>
/// File history snapshot record - checkpoint for undo/restore functionality.
/// </summary>
public record FileHistorySnapshotRecord : SessionRecord
{
    /// <summary>
    /// Message ID this snapshot is associated with.
    /// </summary>
    public required string MessageId { get; init; }

    /// <summary>
    /// Whether this is an update to an existing snapshot.
    /// </summary>
    public bool IsSnapshotUpdate { get; init; }

    /// <summary>
    /// Snapshot data with file backups.
    /// </summary>
    public required FileSnapshot Snapshot { get; init; }
}

/// <summary>
/// Snapshot data containing file backup information.
/// </summary>
public record FileSnapshot
{
    /// <summary>
    /// Message ID for this snapshot.
    /// </summary>
    public required string MessageId { get; init; }

    /// <summary>
    /// Timestamp when snapshot was created.
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Map of original file paths to their backup info.
    /// Key: original file path, Value: backup information.
    /// </summary>
    public required IReadOnlyDictionary<string, FileBackupInfo> TrackedFileBackups { get; init; }
}

/// <summary>
/// Backup information for a single file.
/// </summary>
public record FileBackupInfo
{
    /// <summary>
    /// Backup file name in ~/.claude/file-history/{session-id}/
    /// Format: {hash}@v{version}
    /// </summary>
    public string? BackupFileName { get; init; }

    /// <summary>
    /// When the backup was created.
    /// </summary>
    public DateTime BackupTime { get; init; }

    /// <summary>
    /// Version number of this backup.
    /// </summary>
    public int Version { get; init; }
}
