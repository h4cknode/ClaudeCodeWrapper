# ClaudeCodeWrapper

C# wrapper for **Claude Code CLI** with full session support.

[![CI Build](https://github.com/hysaordis/ClaudeCodeWrapper/actions/workflows/ci.yml/badge.svg)](https://github.com/hysaordis/ClaudeCodeWrapper/actions/workflows/ci.yml)
[![Release](https://github.com/hysaordis/ClaudeCodeWrapper/actions/workflows/release.yml/badge.svg)](https://github.com/hysaordis/ClaudeCodeWrapper/actions/workflows/release.yml)
[![NuGet](https://img.shields.io/nuget/v/ClaudeCodeWrapper.svg)](https://www.nuget.org/packages/ClaudeCodeWrapper)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ClaudeCodeWrapper.svg)](https://www.nuget.org/packages/ClaudeCodeWrapper)

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

### With Metrics

```csharp
var response = await claude.SendWithResponseAsync("Explain this code");

Console.WriteLine($"Response: {response.Content}");
Console.WriteLine($"Tokens: {response.Tokens?.Input} in, {response.Tokens?.Output} out");
Console.WriteLine($"Session: {response.SessionId}");
```

---

## Streaming

### Stream Activities

Monitor tool calls and results in real-time:

```csharp
var response = await claude.StreamWithResponseAsync(
    "Refactor this code",
    activity => Console.WriteLine($"[{activity.Type}] {activity.Tool}: {activity.Summary}")
);
```

### Stream Activities (Async Handler)

Use async callbacks for I/O operations:

```csharp
var response = await claude.StreamWithResponseAsync(
    "Analyze this codebase",
    async activity =>
    {
        await SaveToLogAsync(activity);
        Console.WriteLine($"[{activity.Type}] {activity.Summary}");
    }
);
```

### Stream Raw Records

Access complete session records with full detail:

```csharp
await claude.StreamRecordsAsync(prompt, record =>
{
    switch (record)
    {
        case AssistantRecord ar:
            foreach (var block in ar.Message.Content)
            {
                if (block is ToolUseBlock tool)
                    Console.WriteLine($"Tool: {tool.Name} (ID: {tool.Id})");
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

### Stream Raw Records with Response

```csharp
var response = await claude.StreamRecordsWithResponseAsync(
    prompt,
    record => ProcessRecord(record)
);

Console.WriteLine($"Session: {response.SessionId}");
Console.WriteLine($"Tokens: {response.Tokens?.Input} in, {response.Tokens?.Output} out");
```

### Stream Raw Records (Async Handler)

```csharp
var response = await claude.StreamRecordsWithResponseAsync(
    prompt,
    async record =>
    {
        await ProcessRecordAsync(record);
    }
);
```

---

## Session Management

### Resume a Session

```csharp
// Simple resume
var result = await claude.ResumeAsync("session-id", "Continue the task");

// Resume with detailed response
var response = await claude.ResumeWithResponseAsync("session-id", "Continue...");
Console.WriteLine($"Tokens used: {response.Tokens?.Input}");

// Resume with activity streaming
var response = await claude.ResumeWithStreamingAsync(
    "session-id",
    "Continue the refactoring",
    activity => Console.WriteLine($"[{activity.Type}] {activity.Summary}")
);

// Resume with async activity streaming
var response = await claude.ResumeWithStreamingAsync(
    "session-id",
    "Continue...",
    async activity => await LogActivityAsync(activity)
);

// Resume with full record streaming
var response = await claude.ResumeWithRecordsStreamingAsync(
    "session-id",
    "Continue...",
    record => ProcessRecord(record)
);

// Resume with async record streaming
var response = await claude.ResumeWithRecordsStreamingAsync(
    "session-id",
    "Continue...",
    async record => await ProcessRecordAsync(record)
);
```

### Continue Last Session

```csharp
// Simple continue
var result = await claude.ContinueAsync("What were we discussing?");

// Continue with detailed response
var response = await claude.ContinueWithResponseAsync("Continue the task");
Console.WriteLine($"Tokens: {response.Tokens?.Input}");

// Continue with activity streaming
var response = await claude.ContinueWithStreamingAsync(
    "Continue...",
    activity => Console.WriteLine($"[{activity.Type}] {activity.Summary}")
);

// Continue with async activity streaming
var response = await claude.ContinueWithStreamingAsync(
    "Continue...",
    async activity => await LogActivityAsync(activity)
);
```

---

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

---

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

---

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
catch (ClaudeNotInstalledException ex)
{
    // Claude CLI not found
    Console.WriteLine($"Install Claude Code: npm install -g @anthropic-ai/claude-code");
}
catch (ClaudeCodeException ex)
{
    // General CLI error
    Console.WriteLine($"Error: {ex.Message}");
}
```

---

## API Reference

### Send Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `SendAsync(prompt)` | `string` | Send prompt, get text response |
| `SendWithResponseAsync(prompt)` | `Response` | Send prompt, get response with metrics |
| `SendWithSessionAsync(prompt)` | `(string, Session?)` | Send prompt, get result and full session |

### Stream Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `StreamAsync(prompt, onActivity)` | `string` | Stream activities, get text |
| `StreamWithResponseAsync(prompt, onActivity)` | `Response` | Stream activities, get response |
| `StreamWithResponseAsync(prompt, onActivityAsync)` | `Response` | Stream with async handler |
| `StreamRecordsAsync(prompt, onRecord)` | `string` | Stream raw records, get text |
| `StreamRecordsWithResponseAsync(prompt, onRecord)` | `Response` | Stream records, get response |
| `StreamRecordsWithResponseAsync(prompt, onRecordAsync)` | `Response` | Stream records with async handler |

### Resume Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `ResumeAsync(sessionId, prompt)` | `string` | Resume session, get text |
| `ResumeWithResponseAsync(sessionId, prompt)` | `Response` | Resume with metrics |
| `ResumeWithStreamingAsync(sessionId, prompt, onActivity)` | `Response` | Resume with activity streaming |
| `ResumeWithStreamingAsync(sessionId, prompt, onActivityAsync)` | `Response` | Resume with async streaming |
| `ResumeWithRecordsStreamingAsync(sessionId, prompt, onRecord)` | `Response` | Resume with record streaming |
| `ResumeWithRecordsStreamingAsync(sessionId, prompt, onRecordAsync)` | `Response` | Resume with async record streaming |

### Continue Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `ContinueAsync(prompt)` | `string` | Continue last session, get text |
| `ContinueWithResponseAsync(prompt)` | `Response` | Continue with metrics |
| `ContinueWithStreamingAsync(prompt, onActivity)` | `Response` | Continue with activity streaming |
| `ContinueWithStreamingAsync(prompt, onActivityAsync)` | `Response` | Continue with async streaming |

### Session Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `LoadSessionAsync(sessionId)` | `Session?` | Load complete session data |
| `ListSessions(projectPath?)` | `IEnumerable<SessionInfo>` | List available sessions |
| `GetSessionRepository()` | `SessionRepository` | Get repository for advanced access |

### Usage Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `GetUsageAsync()` | `UsageInfo?` | Get current usage info |
| `IsWithinLimitsAsync(session, weekly)` | `bool` | Check if within thresholds |

---

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
| `ToolUseBlock` | Tool invocation (with `Id` for correlation) |
| `ToolResultBlock` | Tool execution result (with `ToolUseId`) |
| `ImageBlock` | Base64 encoded image |

## Permission Modes

| Mode | Description |
|------|-------------|
| `PermissionMode.Default` | Standard - asks permission for writes |
| `PermissionMode.Plan` | Read-only, no modifications |
| `PermissionMode.AcceptEdits` | Auto-accept file edits |
| `PermissionMode.BypassPermissions` | No permission prompts |

---

## Configuration Options

```csharp
var options = new ClaudeCodeOptions
{
    // Path to Claude CLI (auto-detected if null)
    ClaudePath = null,

    // Model selection
    Model = "sonnet",  // sonnet, opus, haiku

    // System prompts
    SystemPrompt = "Custom system prompt",
    AppendSystemPrompt = "Appended to default prompt",

    // Working directory for file operations
    WorkingDirectory = "/path/to/project",

    // Permission handling
    PermissionMode = PermissionMode.AcceptEdits,

    // Agent limits
    MaxTurns = 0,  // 0 = unlimited

    // Tool filtering
    AllowedTools = new List<string> { "Read", "Grep", "Glob" },
    DisallowedTools = new List<string> { "Bash" },

    // Environment variables
    EnvironmentVariables = new Dictionary<string, string>
    {
        ["MY_VAR"] = "value"
    },

    // Usage monitoring
    EnableUsageMonitoring = true,
    UsageCacheExpiry = TimeSpan.FromMinutes(1)
};
```

## License

MIT
