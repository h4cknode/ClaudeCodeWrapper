# Changelog

## v1.2.0 - Full Session Schema Support

Complete rewrite of session handling to support the full Claude Code session schema.

### Added

- **Session aggregate**: Load complete sessions with all related data
  - `LoadSessionAsync()` - Load a session by ID
  - `SendWithSessionAsync()` - Execute and get full session object
  - `ListSessions()` - List all sessions
  - `GetSessionRepository()` - Direct repository access

- **SessionRepository**: Access all session-related files
  - Main session file (`.jsonl`)
  - Sub-agent files (`agent-*.jsonl`)
  - Todo files (`~/.claude/todos/`)
  - File history backups (`~/.claude/file-history/`)
  - Debug logs (`~/.claude/debug/`)

- **SessionRecordParser**: Full JSONL parsing for all record types

- **New record types**:
  - `UserRecord` - User messages and tool results
  - `AssistantRecord` - AI responses with content blocks and usage
  - `SummaryRecord` - Conversation summaries
  - `SystemRecord` - System messages and metadata
  - `QueueOperationRecord` - Queue management
  - `FileHistorySnapshotRecord` - File checkpoints for undo

- **Content block types**:
  - `TextBlock` - Plain text response
  - `ThinkingBlock` - Extended thinking/reasoning
  - `ToolUseBlock` - Tool invocation with typed input
  - `ToolResultBlock` - Tool execution result
  - `ImageBlock` - Base64 encoded images

- **TokenUsage**: Detailed token tracking
  - Input/output tokens
  - Cache creation/read tokens
  - Cache hit rate calculation
  - Server tool use (web search, web fetch)

- **Session computed properties**:
  - `TotalTokens`, `TotalInputTokens`, `TotalOutputTokens`
  - `TotalCacheReadTokens`, `AverageCacheHitRate`
  - `ToolUsageCounts`, `ModelUsage`
  - `TotalWebSearchRequests`, `TotalWebFetchRequests`
  - `ModifiedFiles`, `HasErrors`
  - `GetThread()`, `GetChildren()`, `RootMessages`

- **New streaming methods**:
  - `StreamRecordsAsync()` - Stream raw SessionRecord objects
  - `StreamRecordsWithResponseAsync()` - Stream records with Response

### Changed

- `SessionMonitor` now emits `SessionRecord` instead of `SessionActivity`
- `Activity` now created from `SessionRecord` via `Activity.FromRecord()`
- `Activity` includes `OriginalRecord` reference for full access

### Removed

- `SessionActivity` class (replaced by `SessionRecord` hierarchy)

---

## v1.1.0 - Usage Monitoring & Error Handling

### Added

- **UsageMonitor service**: Track API usage limits in real-time
  - 5-hour session limit monitoring
  - 7-day weekly limit monitoring
  - Automatic credential retrieval from macOS Keychain or credentials file
  - Configurable cache expiry
- **Rate limit exceptions**:
  - `RateLimitException` for HTTP 429 errors (with RequestId, ErrorType, RetryAfter)
  - `OverloadedException` for HTTP 529 errors
- **Automatic error detection**: CLI output is parsed for rate limit errors
- **New options**:
  - `EnableUsageMonitoring` - Enable/disable usage monitoring
  - `UsageCacheExpiry` - Configure cache duration
- **New methods**:
  - `GetUsageAsync()` - Get current usage info
  - `IsWithinLimitsAsync()` - Check if within safe limits

### Models

- `UsageInfo` - Usage data (FiveHour, SevenDay limits)
- `SessionUsage` - Individual limit info (Utilization, ResetsAt, Available)

---

## v1.0.0 - Initial Release

C# wrapper for Claude Code CLI.

### Features

- **Simple API**: `ClaudeCode.Initialize()`, `SendAsync()`, `StreamAsync()`
- **Real-time activity monitoring** with sub-agent support
- **Smart PermissionMode enum**:
  - `PermissionMode.Default` - Standard permissions
  - `PermissionMode.Plan` - Read-only analysis mode
  - `PermissionMode.AcceptEdits` - Auto-accept file edits
  - `PermissionMode.BypassPermissions` - No permission prompts
- **Response metrics**: model, session ID, duration, token usage
- **Session management**: Resume and continue sessions

### Requirements

- .NET 8.0+
- Claude Code CLI installed
- Claude Code Max subscription
