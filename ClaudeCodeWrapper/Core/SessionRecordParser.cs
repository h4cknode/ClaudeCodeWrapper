using System.Text.Json;
using ClaudeCodeWrapper.Models.Blocks;
using ClaudeCodeWrapper.Models.Records;

namespace ClaudeCodeWrapper.Core;

// Alias to avoid conflict with ClaudeCodeWrapper.TokenUsage in Response.cs
using TokenUsage = ClaudeCodeWrapper.Models.TokenUsage;
using CacheCreation = ClaudeCodeWrapper.Models.CacheCreation;
using ServerToolUse = ClaudeCodeWrapper.Models.ServerToolUse;

/// <summary>
/// Parser for Claude Code session JSONL records.
/// </summary>
public static class SessionRecordParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Parse a JSONL line into a SessionRecord.
    /// </summary>
    public static SessionRecord? Parse(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return null;

        try
        {
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;

            var type = root.TryGetProperty("type", out var t) ? t.GetString() : null;

            return type switch
            {
                "user" => ParseUserRecord(root),
                "assistant" => ParseAssistantRecord(root),
                "summary" => ParseSummaryRecord(root),
                "system" => ParseSystemRecord(root),
                "queue-operation" => ParseQueueOperationRecord(root),
                "file-history-snapshot" => ParseFileHistorySnapshotRecord(root),
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Parse multiple JSONL lines.
    /// </summary>
    public static IEnumerable<SessionRecord> ParseMany(IEnumerable<string> lines)
    {
        foreach (var line in lines)
        {
            var record = Parse(line);
            if (record != null)
                yield return record;
        }
    }

    /// <summary>
    /// Parse a JSONL file.
    /// </summary>
    public static async Task<IReadOnlyList<SessionRecord>> ParseFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            return [];

        var lines = await File.ReadAllLinesAsync(filePath, cancellationToken);
        return ParseMany(lines).ToList();
    }

    #region Record Parsers

    private static UserRecord ParseUserRecord(JsonElement root)
    {
        return new UserRecord
        {
            Type = "user",
            Timestamp = ParseTimestamp(root),
            SessionId = GetStringOrNull(root, "sessionId"),
            Uuid = GetStringOrNull(root, "uuid"),
            ParentUuid = GetStringOrNull(root, "parentUuid"),
            Cwd = GetStringOrNull(root, "cwd"),
            GitBranch = GetStringOrNull(root, "gitBranch"),
            Version = GetStringOrNull(root, "version"),
            UserType = GetStringOrNull(root, "userType"),
            IsSidechain = GetBoolOrDefault(root, "isSidechain"),
            AgentId = GetStringOrNull(root, "agentId"),
            Slug = GetStringOrNull(root, "slug"),
            Message = ParseUserMessage(root),
            ToolUseResult = ParseToolUseResult(root),
            ThinkingMetadata = ParseThinkingMetadata(root),
            Todos = ParseTodos(root),
            IsMeta = GetBoolOrDefault(root, "isMeta"),
            IsVisibleInTranscriptOnly = GetBoolOrDefault(root, "isVisibleInTranscriptOnly"),
            IsCompactSummary = GetBoolOrDefault(root, "isCompactSummary")
        };
    }

    private static AssistantRecord ParseAssistantRecord(JsonElement root)
    {
        return new AssistantRecord
        {
            Type = "assistant",
            Timestamp = ParseTimestamp(root),
            SessionId = GetStringOrNull(root, "sessionId"),
            Uuid = GetStringOrNull(root, "uuid"),
            ParentUuid = GetStringOrNull(root, "parentUuid"),
            Cwd = GetStringOrNull(root, "cwd"),
            GitBranch = GetStringOrNull(root, "gitBranch"),
            Version = GetStringOrNull(root, "version"),
            UserType = GetStringOrNull(root, "userType"),
            IsSidechain = GetBoolOrDefault(root, "isSidechain"),
            AgentId = GetStringOrNull(root, "agentId"),
            Slug = GetStringOrNull(root, "slug"),
            Message = ParseAssistantMessage(root),
            RequestId = GetStringOrNull(root, "requestId"),
            IsApiErrorMessage = GetBoolOrDefault(root, "isApiErrorMessage"),
            Error = GetStringOrNull(root, "error")
        };
    }

    private static SummaryRecord ParseSummaryRecord(JsonElement root)
    {
        return new SummaryRecord
        {
            Type = "summary",
            Timestamp = ParseTimestamp(root),
            SessionId = GetStringOrNull(root, "sessionId"),
            Summary = GetStringOrNull(root, "summary") ?? "",
            LeafUuid = GetStringOrNull(root, "leafUuid") ?? ""
        };
    }

    private static SystemRecord ParseSystemRecord(JsonElement root)
    {
        return new SystemRecord
        {
            Type = "system",
            Timestamp = ParseTimestamp(root),
            SessionId = GetStringOrNull(root, "sessionId"),
            Uuid = GetStringOrNull(root, "uuid"),
            ParentUuid = GetStringOrNull(root, "parentUuid"),
            Cwd = GetStringOrNull(root, "cwd"),
            GitBranch = GetStringOrNull(root, "gitBranch"),
            Version = GetStringOrNull(root, "version"),
            UserType = GetStringOrNull(root, "userType"),
            IsSidechain = GetBoolOrDefault(root, "isSidechain"),
            AgentId = GetStringOrNull(root, "agentId"),
            Slug = GetStringOrNull(root, "slug"),
            Subtype = GetStringOrNull(root, "subtype") ?? "",
            Content = GetStringOrNull(root, "content") ?? "",
            Level = GetStringOrNull(root, "level"),
            LogicalParentUuid = GetStringOrNull(root, "logicalParentUuid"),
            CompactMetadata = ParseCompactMetadata(root)
        };
    }

    private static QueueOperationRecord ParseQueueOperationRecord(JsonElement root)
    {
        return new QueueOperationRecord
        {
            Type = "queue-operation",
            Timestamp = ParseTimestamp(root),
            SessionId = GetStringOrNull(root, "sessionId"),
            Operation = GetStringOrNull(root, "operation") ?? "",
            Content = GetStringOrNull(root, "content")
        };
    }

    private static FileHistorySnapshotRecord ParseFileHistorySnapshotRecord(JsonElement root)
    {
        return new FileHistorySnapshotRecord
        {
            Type = "file-history-snapshot",
            MessageId = GetStringOrNull(root, "messageId") ?? "",
            IsSnapshotUpdate = GetBoolOrDefault(root, "isSnapshotUpdate"),
            Snapshot = ParseFileSnapshot(root)
        };
    }

    #endregion

    #region Message Parsers

    private static UserMessage ParseUserMessage(JsonElement root)
    {
        if (!root.TryGetProperty("message", out var msg))
            return new UserMessage();

        string? contentString = null;
        List<ContentBlock>? contentBlocks = null;

        if (msg.TryGetProperty("content", out var content))
        {
            if (content.ValueKind == JsonValueKind.String)
            {
                contentString = content.GetString();
            }
            else if (content.ValueKind == JsonValueKind.Array)
            {
                contentBlocks = [];
                foreach (var item in content.EnumerateArray())
                {
                    var block = ParseContentBlock(item);
                    if (block != null)
                        contentBlocks.Add(block);
                }
            }
        }

        return new UserMessage
        {
            Role = "user",
            ContentString = contentString,
            ContentBlocks = contentBlocks
        };
    }

    private static AssistantMessage ParseAssistantMessage(JsonElement root)
    {
        if (!root.TryGetProperty("message", out var msg))
            return new AssistantMessage
            {
                Model = "unknown",
                Content = []
            };

        var content = new List<ContentBlock>();
        if (msg.TryGetProperty("content", out var contentArray) &&
            contentArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in contentArray.EnumerateArray())
            {
                var block = ParseContentBlock(item);
                if (block != null)
                    content.Add(block);
            }
        }

        return new AssistantMessage
        {
            Model = GetStringOrNull(msg, "model") ?? "unknown",
            Id = GetStringOrNull(msg, "id"),
            MessageType = GetStringOrNull(msg, "type") ?? "message",
            Role = GetStringOrNull(msg, "role") ?? "assistant",
            Content = content,
            StopReason = GetStringOrNull(msg, "stop_reason"),
            StopSequence = GetStringOrNull(msg, "stop_sequence"),
            Usage = ParseUsage(msg),
            ContextManagement = ParseContextManagement(msg)
        };
    }

    #endregion

    #region Content Block Parsers

    private static ContentBlock? ParseContentBlock(JsonElement element)
    {
        var type = GetStringOrNull(element, "type");

        return type switch
        {
            "text" => new TextBlock
            {
                Type = "text",
                Text = GetStringOrNull(element, "text") ?? ""
            },
            "thinking" => new ThinkingBlock
            {
                Type = "thinking",
                Thinking = GetStringOrNull(element, "thinking") ?? ""
            },
            "tool_use" => new ToolUseBlock
            {
                Type = "tool_use",
                Id = GetStringOrNull(element, "id") ?? "",
                Name = GetStringOrNull(element, "name") ?? "",
                Input = element.TryGetProperty("input", out var input) ? input.Clone() : null,
                Signature = GetStringOrNull(element, "signature")
            },
            "tool_result" => new ToolResultBlock
            {
                Type = "tool_result",
                ToolUseId = GetStringOrNull(element, "tool_use_id") ?? "",
                Content = element.TryGetProperty("content", out var c)
                    ? (c.ValueKind == JsonValueKind.String ? c.GetString() : c.GetRawText())
                    : null,
                IsError = GetBoolOrDefault(element, "is_error")
            },
            "image" => new ImageBlock
            {
                Type = "image",
                Source = ParseImageSource(element)
            },
            _ => null
        };
    }

    private static ImageSource ParseImageSource(JsonElement element)
    {
        if (!element.TryGetProperty("source", out var source))
            return new ImageSource { Type = "base64", MediaType = "image/png", Data = "" };

        return new ImageSource
        {
            Type = GetStringOrNull(source, "type") ?? "base64",
            MediaType = GetStringOrNull(source, "media_type") ?? "image/png",
            Data = GetStringOrNull(source, "data") ?? ""
        };
    }

    #endregion

    #region Utility Parsers

    private static TokenUsage? ParseUsage(JsonElement element)
    {
        if (!element.TryGetProperty("usage", out var usage))
            return null;

        return new TokenUsage
        {
            InputTokens = GetIntOrDefault(usage, "input_tokens"),
            OutputTokens = GetIntOrDefault(usage, "output_tokens"),
            CacheCreationInputTokens = GetIntOrDefault(usage, "cache_creation_input_tokens"),
            CacheReadInputTokens = GetIntOrDefault(usage, "cache_read_input_tokens"),
            ServiceTier = GetStringOrNull(usage, "service_tier"),
            CacheCreation = ParseCacheCreation(usage),
            ServerToolUse = ParseServerToolUse(usage)
        };
    }

    private static CacheCreation? ParseCacheCreation(JsonElement usage)
    {
        if (!usage.TryGetProperty("cache_creation", out var cc))
            return null;

        return new CacheCreation
        {
            Ephemeral5mInputTokens = GetIntOrDefault(cc, "ephemeral_5m_input_tokens"),
            Ephemeral1hInputTokens = GetIntOrDefault(cc, "ephemeral_1h_input_tokens")
        };
    }

    private static ServerToolUse? ParseServerToolUse(JsonElement usage)
    {
        if (!usage.TryGetProperty("server_tool_use", out var stu))
            return null;

        return new ServerToolUse
        {
            WebSearchRequests = GetIntOrDefault(stu, "web_search_requests"),
            WebFetchRequests = GetIntOrDefault(stu, "web_fetch_requests")
        };
    }

    private static ContextManagement? ParseContextManagement(JsonElement msg)
    {
        if (!msg.TryGetProperty("context_management", out var cm) ||
            cm.ValueKind == JsonValueKind.Null)
            return null;

        return new ContextManagement
        {
            Truncated = GetBoolOrDefault(cm, "truncated"),
            Reason = GetStringOrNull(cm, "reason")
        };
    }

    private static ToolUseResultInfo? ParseToolUseResult(JsonElement root)
    {
        if (!root.TryGetProperty("toolUseResult", out var tur))
            return null;

        return new ToolUseResultInfo
        {
            Stdout = GetStringOrNull(tur, "stdout"),
            Stderr = GetStringOrNull(tur, "stderr"),
            Interrupted = GetBoolOrDefault(tur, "interrupted"),
            IsImage = GetBoolOrDefault(tur, "isImage"),
            Status = GetStringOrNull(tur, "status"),
            Prompt = GetStringOrNull(tur, "prompt")
        };
    }

    private static ThinkingMetadata? ParseThinkingMetadata(JsonElement root)
    {
        if (!root.TryGetProperty("thinkingMetadata", out var tm))
            return null;

        return new ThinkingMetadata
        {
            Enabled = GetBoolOrDefault(tm, "enabled"),
            BudgetTokens = tm.TryGetProperty("budgetTokens", out var bt) ? bt.GetInt32() : null
        };
    }

    private static IReadOnlyList<TodoItem>? ParseTodos(JsonElement root)
    {
        if (!root.TryGetProperty("todos", out var todos) ||
            todos.ValueKind != JsonValueKind.Array)
            return null;

        var result = new List<TodoItem>();
        foreach (var item in todos.EnumerateArray())
        {
            result.Add(new TodoItem
            {
                Content = GetStringOrNull(item, "content") ?? "",
                Status = GetStringOrNull(item, "status") ?? "pending",
                ActiveForm = GetStringOrNull(item, "activeForm") ?? ""
            });
        }

        return result;
    }

    private static CompactMetadata? ParseCompactMetadata(JsonElement root)
    {
        if (!root.TryGetProperty("compactMetadata", out var cm))
            return null;

        return new CompactMetadata
        {
            MessageCount = cm.TryGetProperty("messageCount", out var mc) ? mc.GetInt32() : null,
            TotalTokens = cm.TryGetProperty("totalTokens", out var tt) ? tt.GetInt32() : null
        };
    }

    private static FileSnapshot ParseFileSnapshot(JsonElement root)
    {
        if (!root.TryGetProperty("snapshot", out var snapshot))
            return new FileSnapshot
            {
                MessageId = "",
                TrackedFileBackups = new Dictionary<string, FileBackupInfo>()
            };

        var backups = new Dictionary<string, FileBackupInfo>();
        if (snapshot.TryGetProperty("trackedFileBackups", out var tfb) &&
            tfb.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in tfb.EnumerateObject())
            {
                backups[prop.Name] = new FileBackupInfo
                {
                    BackupFileName = GetStringOrNull(prop.Value, "backupFileName"),
                    BackupTime = ParseTimestamp(prop.Value) ?? DateTime.MinValue,
                    Version = GetIntOrDefault(prop.Value, "version")
                };
            }
        }

        return new FileSnapshot
        {
            MessageId = GetStringOrNull(snapshot, "messageId") ?? "",
            Timestamp = ParseTimestamp(snapshot) ?? DateTime.MinValue,
            TrackedFileBackups = backups
        };
    }

    #endregion

    #region Helpers

    private static string? GetStringOrNull(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var value) &&
               value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static bool GetBoolOrDefault(JsonElement element, string property, bool defaultValue = false)
    {
        return element.TryGetProperty(property, out var value) &&
               value.ValueKind == JsonValueKind.True;
    }

    private static int GetIntOrDefault(JsonElement element, string property, int defaultValue = 0)
    {
        return element.TryGetProperty(property, out var value) &&
               value.ValueKind == JsonValueKind.Number
            ? value.GetInt32()
            : defaultValue;
    }

    private static DateTime? ParseTimestamp(JsonElement element)
    {
        var ts = GetStringOrNull(element, "timestamp");
        if (ts == null) return null;

        return DateTime.TryParse(ts, out var result) ? result : null;
    }

    #endregion
}
