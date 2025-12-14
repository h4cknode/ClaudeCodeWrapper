# ClaudeCodeWrapper

C# wrapper for **Claude Code CLI** with full session support.

## Requirements

- .NET 8.0+
- [Claude Code CLI](https://www.anthropic.com/claude-code) installed (`npm install -g @anthropic-ai/claude-code`)

## Installation

```bash
dotnet add package ClaudeCodeWrapper
```

## Quick Start

```csharp
using ClaudeCodeWrapper;

// Initialize
var claude = ClaudeCode.Initialize();

// Send a message
var response = await claude.SendAsync("Hello Claude!");
```

## Usage

### With Options

```csharp
var claude = ClaudeCode.Initialize(new ClaudeCodeOptions
{
    Model = "sonnet",
    WorkingDirectory = "/path/to/project",
    SystemPrompt = "You are a helpful assistant",
    PermissionMode = PermissionMode.AcceptEdits
});
```

### Stream Activities

Monitor tool calls and results in real-time:

```csharp
var response = await claude.StreamWithResponseAsync(
    "Refactor this code",
    activity => Console.WriteLine($"[{activity.Type}] {activity.Tool}: {activity.Summary}")
);
```

### With Metrics

```csharp
var response = await claude.SendWithResponseAsync("Explain this code");

Console.WriteLine($"Response: {response.Content}");
Console.WriteLine($"Tokens: {response.Tokens?.Input} in, {response.Tokens?.Output} out");
Console.WriteLine($"Session: {response.SessionId}");
```

### Session Management

```csharp
// Resume a previous session
var response = await claude.ResumeAsync("session-id", "Continue...");

// Continue the last session
var response = await claude.ContinueAsync("What were we discussing?");
```

## Full Session Support

Load complete sessions with all related data (records, agents, todos, file history).

### Load a Session

```csharp
using ClaudeCodeWrapper.Models;
using ClaudeCodeWrapper.Models.Records;

// Load a complete session
var session = await claude.LoadSessionAsync("session-uuid");

// Access session metadata
Console.WriteLine($"Session: {session.Slug}");
Console.WriteLine($"Started: {session.StartedAt}");
Console.WriteLine($"Messages: {session.MessageCount}");
Console.WriteLine($"Total tokens: {session.TotalTokens}");
```

### Session Data

```csharp
// All records (user, assistant, summary, system, etc.)
foreach (var record in session.Records)
{
    Console.WriteLine($"{record.Type}: {record.Uuid}");
}

// Filter by type
foreach (var assistant in session.AssistantRecords)
{
    Console.WriteLine($"Model: {assistant.Message.Model}");
    Console.WriteLine($"Tokens: {assistant.Message.Usage?.TotalTokens}");
    Console.WriteLine($"Cache hit: {assistant.Message.Usage?.CacheHitRate:P0}");
}

// Sub-agents spawned by Task tool
foreach (var agent in session.Agents)
{
    Console.WriteLine($"Agent {agent.AgentId}: {agent.Records.Count} records");
}

// Current todo list
foreach (var todo in session.Todos)
{
    Console.WriteLine($"[{todo.Status}] {todo.Content}");
}

// Files modified (with undo history)
foreach (var file in session.ModifiedFiles)
{
    Console.WriteLine($"Modified: {file}");
}
```

### Session Statistics

```csharp
// Token usage
Console.WriteLine($"Input tokens: {session.TotalInputTokens}");
Console.WriteLine($"Output tokens: {session.TotalOutputTokens}");
Console.WriteLine($"Cache reads: {session.TotalCacheReadTokens}");
Console.WriteLine($"Cache hit rate: {session.AverageCacheHitRate:P0}");

// Tool usage counts
foreach (var (tool, count) in session.ToolUsageCounts)
{
    Console.WriteLine($"{tool}: {count}x");
}

// Model usage
foreach (var (model, count) in session.ModelUsage)
{
    Console.WriteLine($"{model}: {count} responses");
}

// Web operations
Console.WriteLine($"Web searches: {session.TotalWebSearchRequests}");
Console.WriteLine($"Web fetches: {session.TotalWebFetchRequests}");
```

### Stream with Full Records

Access complete record data during streaming:

```csharp
await claude.StreamRecordsAsync(prompt, record =>
{
    switch (record)
    {
        case AssistantRecord ar:
            foreach (var block in ar.Message.Content)
            {
                if (block is ToolUseBlock tool)
                    Console.WriteLine($"Tool: {tool.Name}");
                else if (block is TextBlock text)
                    Console.WriteLine($"Text: {text.Text}");
                else if (block is ThinkingBlock thinking)
                    Console.WriteLine($"Thinking: {thinking.Thinking}");
            }
            break;

        case UserRecord ur when ur.Message.IsToolResults:
            Console.WriteLine("Tool results received");
            break;
    }
});
```

### Get Session After Execution

```csharp
var (result, session) = await claude.SendWithSessionAsync("Your prompt");

// Access the complete session data
Console.WriteLine($"Session ID: {session?.Id}");
Console.WriteLine($"Total tokens used: {session?.TotalTokens}");
```

### List All Sessions

```csharp
// List all sessions
foreach (var info in claude.ListSessions())
{
    Console.WriteLine($"{info.SessionId} - {info.ModifiedAt} ({info.SizeBytes} bytes)");
}

// List sessions for a specific project
foreach (var info in claude.ListSessions("/path/to/project"))
{
    Console.WriteLine($"{info.SessionId}");
}
```

### Session Repository

For advanced use cases, access the repository directly:

```csharp
var repo = claude.GetSessionRepository();

// Find session file
var path = repo.FindSessionFile("session-uuid");

// Read debug log
var debugLog = await repo.ReadDebugLogAsync("session-uuid");
```

## Record Types

| Type | Description |
|------|-------------|
| `UserRecord` | User messages and tool results |
| `AssistantRecord` | AI responses with content blocks |
| `SummaryRecord` | Conversation summaries |
| `SystemRecord` | System messages and metadata |
| `QueueOperationRecord` | Queue management |
| `FileHistorySnapshotRecord` | File checkpoints for undo |

## Content Blocks

| Type | Description |
|------|-------------|
| `TextBlock` | Plain text response |
| `ThinkingBlock` | Extended thinking/reasoning |
| `ToolUseBlock` | Tool invocation |
| `ToolResultBlock` | Tool execution result |
| `ImageBlock` | Base64 encoded image |

## Permission Modes

| Mode | Description |
|------|-------------|
| `PermissionMode.Default` | Standard - asks permission for writes |
| `PermissionMode.Plan` | Read-only, no modifications |
| `PermissionMode.AcceptEdits` | Auto-accept file edits |
| `PermissionMode.BypassPermissions` | No permission prompts |

## Usage Monitoring

Track your API usage limits:

```csharp
var claude = ClaudeCode.Initialize(new ClaudeCodeOptions
{
    EnableUsageMonitoring = true
});

// Get current usage
var usage = await claude.GetUsageAsync();
if (usage != null)
{
    Console.WriteLine($"Session: {usage.FiveHour.Utilization}% used");
    Console.WriteLine($"Weekly:  {usage.SevenDay.Utilization}% used");
    Console.WriteLine($"Resets:  {usage.FiveHour.ResetsAt}");
}

// Check if within limits
if (await claude.IsWithinLimitsAsync(sessionThreshold: 90, weeklyThreshold: 80))
{
    await claude.SendAsync("Safe to send!");
}
```

## Error Handling

The wrapper throws specific exceptions for rate limit errors:

```csharp
try
{
    var response = await claude.SendAsync(prompt);
}
catch (RateLimitException ex)
{
    // HTTP 429 - Rate limit exceeded
    Console.WriteLine($"Rate limited: {ex.Message}");
    Console.WriteLine($"Request ID: {ex.RequestId}");
}
catch (OverloadedException ex)
{
    // HTTP 529 - API overloaded
    Console.WriteLine($"API overloaded, retry after: {ex.RetryAfter}");
}
```

## License

MIT
