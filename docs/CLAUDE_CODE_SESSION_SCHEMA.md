# Claude Code Session Schema

> Complete schema for Claude Code session files (.jsonl)
> Extracted from **19,246 records** across **443 session files**
> Claude Code versions: 2.0.46 - 2.0.65
> Generated: 2025-12-14

---

## File Location

```
~/.claude/projects/<encoded-project-path>/
├── <session-uuid>.jsonl       # Main session
└── agent-<7-char-id>.jsonl    # Sub-agent logs
```

**Path Encoding**: Forward slashes → hyphens
Example: `/Users/name/project` → `-Users-name-project`

---

## Record Types

| Type | Count | Description |
|------|-------|-------------|
| `user` | 7,023 | User messages + tool results |
| `assistant` | 11,077 | AI responses + tool calls |
| `queue-operation` | 120 | Queue management |
| `summary` | 62 | Conversation summaries |
| `file-history-snapshot` | 921 | File checkpoints for undo |
| `system` | 43 | System messages |

---

## 1. USER Record

```json
{
  "type": "user",
  "timestamp": "2025-12-14T15:14:05.200Z",
  "sessionId": "689db5cd-e60e-45b4-a1bf-c876b242908c",
  "uuid": "104c4cff-d197-4927-820d-9c6c39f33624",
  "parentUuid": null,
  "cwd": "/Users/ordishysa/project",
  "gitBranch": "main",
  "version": "2.0.65",
  "userType": "external",
  "isSidechain": false,
  "message": {
    "role": "user",
    "content": "User message text here"
  }
}
```

### User Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `type` | string | ✓ | Always `"user"` |
| `timestamp` | string | ✓ | ISO 8601 timestamp |
| `sessionId` | string | ✓ | Session UUID |
| `uuid` | string | ✓ | Message UUID |
| `message` | object | ✓ | Message content |
| `parentUuid` | string/null | | Parent message UUID |
| `cwd` | string | | Working directory |
| `gitBranch` | string | | Git branch |
| `version` | string | | Claude Code version |
| `userType` | string | | `"external"` or `"internal"` |
| `isSidechain` | boolean | | True if sub-agent |
| `agentId` | string | | 7-char agent ID |
| `slug` | string | | Session name |
| `toolUseResult` | object | | Tool execution details |
| `thinkingMetadata` | object | | Thinking mode metadata |
| `todos` | array | | Current todo list |

---

## 2. ASSISTANT Record

```json
{
  "type": "assistant",
  "timestamp": "2025-12-14T15:14:12.497Z",
  "sessionId": "689db5cd-e60e-45b4-a1bf-c876b242908c",
  "uuid": "5046657b-fd75-4ae1-86d5-776e8b97db8c",
  "parentUuid": "104c4cff-d197-4927-820d-9c6c39f33624",
  "requestId": "req_011CW6oCZuEpkL5sM8hpedME",
  "message": {
    "model": "claude-opus-4-5-20251101",
    "id": "msg_01VwsxSShLhe49u4m9myiYzq",
    "type": "message",
    "role": "assistant",
    "content": [...],
    "stop_reason": "end_turn",
    "usage": {...}
  }
}
```

### Assistant Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `type` | string | ✓ | Always `"assistant"` |
| `timestamp` | string | ✓ | ISO 8601 timestamp |
| `sessionId` | string | ✓ | Session UUID |
| `uuid` | string | ✓ | Message UUID |
| `message` | object | ✓ | Full API response |
| `requestId` | string | | API request ID |
| `isApiErrorMessage` | boolean | | True if API error |
| `error` | string | | Error type |

---

## 3. Message Content Types

### Text Block
```json
{
  "type": "text",
  "text": "Response text here"
}
```

### Thinking Block
```json
{
  "type": "thinking",
  "thinking": "Internal reasoning..."
}
```

### Tool Use Block
```json
{
  "type": "tool_use",
  "id": "toolu_01Y4R48cFT6ZLjErTmqXPXJh",
  "name": "Read",
  "input": {
    "file_path": "/path/to/file.py"
  }
}
```

### Tool Result Block
```json
{
  "type": "tool_result",
  "tool_use_id": "toolu_01Y4R48cFT6ZLjErTmqXPXJh",
  "content": "File contents here...",
  "is_error": false
}
```

### Image Block
```json
{
  "type": "image",
  "source": {
    "type": "base64",
    "media_type": "image/png",
    "data": "base64-encoded-data..."
  }
}
```

---

## 4. Tools

### File Operations

| Tool | Count | Description |
|------|-------|-------------|
| `Read` | 1,358 | Read file contents |
| `Write` | 172 | Create/overwrite file |
| `Edit` | 1,035 | String replacement edit |
| `Glob` | 319 | Find files by pattern |
| `Grep` | 369 | Search file contents |

### Shell Operations

| Tool | Count | Description |
|------|-------|-------------|
| `Bash` | 2,037 | Execute shell command |
| `BashOutput` | 147 | Get background output |
| `KillShell` | 54 | Kill background shell |

### Agent Operations

| Tool | Count | Description |
|------|-------|-------------|
| `Task` | 41 | Spawn sub-agent |
| `TaskOutput` | 11 | Get agent output |

### Planning

| Tool | Count | Description |
|------|-------|-------------|
| `TodoWrite` | 338 | Manage todo list |
| `ExitPlanMode` | 7 | Exit planning mode |

### Web Operations

| Tool | Count | Description |
|------|-------|-------------|
| `WebSearch` | 58 | Search the web |
| `WebFetch` | 10 | Fetch web page |

### User Interaction

| Tool | Count | Description |
|------|-------|-------------|
| `AskUserQuestion` | 16 | Ask user with options |

---

## 5. Tool Input Schemas

### Read
```json
{
  "file_path": "/absolute/path/to/file",
  "offset": 100,
  "limit": 50
}
```

### Write
```json
{
  "file_path": "/absolute/path/to/file",
  "content": "file contents"
}
```

### Edit
```json
{
  "file_path": "/absolute/path/to/file",
  "old_string": "text to replace",
  "new_string": "replacement text",
  "replace_all": false
}
```

### Bash
```json
{
  "command": "npm install",
  "description": "Install dependencies",
  "timeout": 60000,
  "run_in_background": false
}
```

### Grep
```json
{
  "pattern": "function\\s+\\w+",
  "path": "/path/to/search",
  "glob": "*.ts",
  "output_mode": "content",
  "-A": 5,
  "-B": 2,
  "-i": true
}
```

### Task
```json
{
  "description": "Analyze codebase",
  "prompt": "Full prompt for agent...",
  "subagent_type": "Explore",
  "model": "haiku",
  "run_in_background": true
}
```

### TodoWrite
```json
{
  "todos": [
    {
      "content": "Implement feature X",
      "status": "in_progress",
      "activeForm": "Implementing feature X"
    }
  ]
}
```

---

## 6. Usage Object

```json
{
  "input_tokens": 1500,
  "output_tokens": 200,
  "cache_creation_input_tokens": 5000,
  "cache_read_input_tokens": 1200,
  "service_tier": "standard",
  "cache_creation": {
    "ephemeral_5m_input_tokens": 5000,
    "ephemeral_1h_input_tokens": 0
  },
  "server_tool_use": {
    "web_search_requests": 1,
    "web_fetch_requests": 0
  }
}
```

---

## 7. File History Snapshot

Used for undo/checkpoint functionality:

```json
{
  "type": "file-history-snapshot",
  "messageId": "uuid-of-associated-message",
  "isSnapshotUpdate": false,
  "snapshot": {
    "messageId": "uuid",
    "timestamp": "2025-12-14T15:14:05.200Z",
    "trackedFileBackups": {
      "/path/to/file.ts": {
        "backupFileName": "backup-uuid.ts",
        "backupTime": "2025-12-14T15:14:05.200Z",
        "version": 1
      }
    }
  }
}
```

---

## 8. Models Used

| Model | Count | Description |
|-------|-------|-------------|
| `claude-opus-4-5-20251101` | 6,137 | Opus (most capable) |
| `claude-sonnet-4-5-20250929` | 3,569 | Sonnet (balanced) |
| `claude-haiku-4-5-20251001` | 1,314 | Haiku (fastest) |
| `<synthetic>` | 57 | Internal/synthetic |

---

## 9. Message Threading

Messages are linked via `parentUuid` → `uuid`:

```
User Message (uuid: A, parentUuid: null)
    ↓
Assistant Response (uuid: B, parentUuid: A)
    ↓
Tool Result (uuid: C, parentUuid: B)
    ↓
Assistant Response (uuid: D, parentUuid: C)
```

---

## 10. Sub-Agent Files

When `Task` tool spawns agents, logs go to separate files:

- **File name**: `agent-<7-char-id>.jsonl`
- **Field `agentId`**: Present in all records
- **Field `isSidechain`**: Always `true`

---

## Quick Reference

```bash
# List all sessions for current project
ls ~/.claude/projects/$(pwd | sed 's/\//-/g')/

# Count records in a session
wc -l ~/.claude/projects/*/<session-id>.jsonl

# Extract all user messages
cat session.jsonl | jq 'select(.type == "user")'

# Extract tool usage
cat session.jsonl | jq 'select(.message.content[]?.type == "tool_use") | .message.content[] | select(.type == "tool_use") | .name'
```

---

## Validation

Schema validated against:
- **443 files**
- **19,246 records**
- **100% success rate**
- **0 parse errors**
