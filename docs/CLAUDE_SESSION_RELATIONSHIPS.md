# Claude Code Session Relationships

> Guida completa per estrarre tutte le relazioni di una sessione Claude Code
> Generato: 2025-12-14

---

## Struttura Directory

```
~/.claude/
├── projects/                    # Sessioni e messaggi
│   └── <project-path>/
│       ├── <session-id>.jsonl   # Sessione principale
│       └── agent-<id>.jsonl     # Sub-agent logs
│
├── todos/                       # Stato todo per sessione
│   └── <session-id>-agent-<agent-id>.json
│
├── file-history/               # Backup file modificati
│   └── <session-id>/
│       └── <hash>@v<version>
│
└── debug/                      # Log debug
    └── <session-id>.txt
```

---

## 1. SESSION FILE (Principale)

**Posizione**: `~/.claude/projects/<project-path>/<session-id>.jsonl`

**Formato**: JSONL (una riga JSON per record)

### Record Types

| Type | Descrizione | Campi Principali |
|------|-------------|------------------|
| `user` | Messaggi utente + tool results | message, uuid, parentUuid, timestamp |
| `assistant` | Risposte AI + tool calls | message, uuid, parentUuid, requestId |
| `queue-operation` | Operazioni coda | operation (enqueue/dequeue) |
| `summary` | Riassunto conversazione | summary, leafUuid |
| `file-history-snapshot` | Checkpoint file | messageId, snapshot |
| `system` | Messaggi sistema | subtype, content, level |

### Schema Record USER

```json
{
  "type": "user",
  "uuid": "104c4cff-d197-4927-820d-9c6c39f33624",
  "parentUuid": null,
  "sessionId": "beea46c7-b915-4235-8769-5ffdd0868c3f",
  "timestamp": "2025-12-14T16:12:29.621Z",
  "cwd": "/Users/ordishysa/project",
  "gitBranch": "main",
  "version": "2.0.65",
  "userType": "external",
  "isSidechain": false,
  "message": {
    "role": "user",
    "content": "Testo messaggio utente"
  }
}
```

### Schema Record ASSISTANT

```json
{
  "type": "assistant",
  "uuid": "5046657b-fd75-4ae1-86d5-776e8b97db8c",
  "parentUuid": "104c4cff-d197-4927-820d-9c6c39f33624",
  "sessionId": "beea46c7-b915-4235-8769-5ffdd0868c3f",
  "requestId": "req_011CW6oCZuEpkL5sM8hpedME",
  "timestamp": "2025-12-14T16:12:35.000Z",
  "message": {
    "model": "claude-opus-4-5-20251101",
    "id": "msg_01VwsxSShLhe49u4m9myiYzq",
    "role": "assistant",
    "content": [
      {"type": "text", "text": "Risposta..."},
      {"type": "tool_use", "id": "toolu_xxx", "name": "Read", "input": {...}}
    ],
    "usage": {
      "input_tokens": 1500,
      "output_tokens": 200
    }
  }
}
```

### Schema Record TOOL RESULT (dentro user)

```json
{
  "type": "user",
  "message": {
    "role": "user",
    "content": [
      {
        "type": "tool_result",
        "tool_use_id": "toolu_xxx",
        "content": "Risultato del tool...",
        "is_error": false
      }
    ]
  },
  "toolUseResult": {
    "stdout": "...",
    "stderr": "",
    "interrupted": false
  }
}
```

---

## 2. AGENT FILES (Sub-agent)

**Posizione**: `~/.claude/projects/<project-path>/agent-<agent-id>.jsonl`

**Relazione**: Collegato tramite `sessionId` e `agentId`

### Come Trovare gli Agent

```python
# Gli agent sono spawned via Task tool
# Nel session file, cerca:
{
  "type": "tool_use",
  "name": "Task",
  "input": {
    "subagent_type": "Explore",
    "prompt": "..."
  }
}

# Il risultato contiene l'agentId:
{
  "type": "tool_result",
  "content": "... agentId: a076969 ..."
}
```

### Schema Agent Record

```json
{
  "type": "user",
  "sessionId": "beea46c7-b915-4235-8769-5ffdd0868c3f",
  "agentId": "a076969",
  "isSidechain": true,
  "message": {...}
}
```

---

## 3. TODO FILES

**Posizione**: `~/.claude/todos/<session-id>-agent-<agent-id>.json`

**Relazione**: Nome file contiene session-id

### Schema Todo

```json
[
  {
    "content": "Descrizione task",
    "status": "completed",
    "activeForm": "Completando task..."
  },
  {
    "content": "Altro task",
    "status": "in_progress",
    "activeForm": "Lavorando su..."
  },
  {
    "content": "Task futuro",
    "status": "pending",
    "activeForm": "In attesa..."
  }
]
```

### Status Possibili

| Status | Descrizione |
|--------|-------------|
| `pending` | Non ancora iniziato |
| `in_progress` | In corso |
| `completed` | Completato |

---

## 4. FILE HISTORY (Backup)

**Posizione**: `~/.claude/file-history/<session-id>/<hash>@v<version>`

**Relazione**: Nome cartella = session-id

### Struttura

```
file-history/<session-id>/
├── 8ba31ec3f75fb99d@v1    # Prima versione
├── 8ba31ec3f75fb99d@v2    # Dopo prima modifica
├── 77f1265318f95934@v1    # Altro file
└── 77f1265318f95934@v2    # Versione 2
```

### Come Funziona

1. `<hash>` = hash del path del file originale
2. `@v<n>` = numero versione (incrementale)
3. Contenuto = snapshot completo del file

### Collegamento con Session

Nel session file, cerca `file-history-snapshot`:

```json
{
  "type": "file-history-snapshot",
  "messageId": "uuid-del-messaggio-che-ha-modificato",
  "isSnapshotUpdate": false,
  "snapshot": {
    "messageId": "uuid",
    "timestamp": "2025-12-14T15:14:05.200Z",
    "trackedFileBackups": {
      "/path/to/file.ts": {
        "backupFileName": "8ba31ec3f75fb99d@v1",
        "backupTime": "2025-12-14T15:14:05.200Z",
        "version": 1
      }
    }
  }
}
```

---

## 5. DEBUG LOG

**Posizione**: `~/.claude/debug/<session-id>.txt`

**Relazione**: Nome file = session-id

### Formato

```
2025-12-14T16:12:01.387Z [DEBUG] Messaggio debug...
2025-12-14T16:12:01.442Z [DEBUG] [LSP MANAGER] Altro messaggio...
2025-12-14T16:12:01.536Z [ERROR] Errore...
```

### Pattern Log

| Pattern | Descrizione |
|---------|-------------|
| `[DEBUG]` | Debug generico |
| `[ERROR]` | Errori |
| `[LSP MANAGER]` | Language Server Protocol |
| `[SLOW OPERATION]` | Operazioni lente |

---

## 6. RELAZIONI TRA MESSAGGI

I messaggi sono collegati tramite `uuid` → `parentUuid`:

```
Messaggio 1 (uuid: A, parentUuid: null)
    │
    └── Messaggio 2 (uuid: B, parentUuid: A)
            │
            └── Messaggio 3 (uuid: C, parentUuid: B)
                    │
                    └── Messaggio 4 (uuid: D, parentUuid: C)
```

### Ricostruire la Conversazione

```python
def build_conversation_tree(records):
    by_uuid = {r['uuid']: r for r in records if 'uuid' in r}

    def get_children(parent_uuid):
        return [r for r in records if r.get('parentUuid') == parent_uuid]

    # Trova root (parentUuid = null)
    roots = [r for r in records if r.get('parentUuid') is None and 'uuid' in r]

    return roots, get_children
```

---

## 7. SCRIPT ESTRAZIONE COMPLETA

### Trovare Tutti i File di una Sessione

```bash
SESSION_ID="beea46c7-b915-4235-8769-5ffdd0868c3f"

# 1. Session file
find ~/.claude/projects -name "${SESSION_ID}.jsonl"

# 2. Agent files (cerca sessionId nel contenuto)
grep -l "\"sessionId\":\"${SESSION_ID}\"" ~/.claude/projects/*/agent-*.jsonl

# 3. Todo files
ls ~/.claude/todos/${SESSION_ID}-*.json

# 4. File history
ls ~/.claude/file-history/${SESSION_ID}/

# 5. Debug log
ls ~/.claude/debug/${SESSION_ID}.txt
```

### Script Python Completo

```python
from pathlib import Path
import json

def extract_session_relations(session_id: str):
    """Estrae tutte le relazioni di una sessione."""

    claude_dir = Path.home() / ".claude"
    relations = {
        "session_id": session_id,
        "session_file": None,
        "agent_files": [],
        "todo_files": [],
        "file_history": [],
        "debug_file": None,
    }

    # 1. Trova session file
    for project_dir in (claude_dir / "projects").iterdir():
        session_file = project_dir / f"{session_id}.jsonl"
        if session_file.exists():
            relations["session_file"] = str(session_file)

            # Trova agent files correlati
            for agent_file in project_dir.glob("agent-*.jsonl"):
                with open(agent_file) as f:
                    first_line = f.readline()
                    if session_id in first_line:
                        relations["agent_files"].append(str(agent_file))
            break

    # 2. Trova todo files
    todos_dir = claude_dir / "todos"
    for todo_file in todos_dir.glob(f"{session_id}-*.json"):
        relations["todo_files"].append(str(todo_file))

    # 3. Trova file history
    history_dir = claude_dir / "file-history" / session_id
    if history_dir.exists():
        for backup_file in history_dir.iterdir():
            relations["file_history"].append({
                "file": str(backup_file),
                "size": backup_file.stat().st_size
            })

    # 4. Trova debug file
    debug_file = claude_dir / "debug" / f"{session_id}.txt"
    if debug_file.exists():
        relations["debug_file"] = str(debug_file)

    return relations

# Uso
session = extract_session_relations("beea46c7-b915-4235-8769-5ffdd0868c3f")
print(json.dumps(session, indent=2))
```

---

## 8. DIAGRAMMA RELAZIONI

```
┌─────────────────────────────────────────────────────────────────┐
│                         SESSION                                  │
│                   (session-id: UUID)                            │
└─────────────────────────┬───────────────────────────────────────┘
                          │
          ┌───────────────┼───────────────┬──────────────┐
          │               │               │              │
          ▼               ▼               ▼              ▼
┌─────────────────┐ ┌───────────┐ ┌────────────┐ ┌────────────┐
│ SESSION FILE    │ │ TODOS     │ │ FILE       │ │ DEBUG      │
│ .jsonl          │ │ .json     │ │ HISTORY    │ │ .txt       │
├─────────────────┤ ├───────────┤ ├────────────┤ ├────────────┤
│ • user records  │ │ • content │ │ • backups  │ │ • logs     │
│ • assistant     │ │ • status  │ │ • versions │ │ • errors   │
│ • tool_use      │ │ • active  │ │ • hash@vN  │ │ • timing   │
│ • tool_result   │ │   Form    │ │            │ │            │
│ • summary       │ └───────────┘ └────────────┘ └────────────┘
│ • snapshots     │
└────────┬────────┘
         │
         │ spawns (Task tool)
         ▼
┌─────────────────┐
│ AGENT FILES     │
│ agent-<id>.jsonl│
├─────────────────┤
│ • isSidechain   │
│ • agentId       │
│ • same sessionId│
└─────────────────┘


LINKING:
════════
Session File ←→ Agent Files    : sessionId field
Session File ←→ Todos          : filename contains session-id
Session File ←→ File History   : folder name = session-id
Session File ←→ Debug          : filename = session-id
Messages ←→ Messages           : parentUuid → uuid
Tool Use ←→ Tool Result        : tool_use.id = tool_result.tool_use_id
Snapshot ←→ File History       : snapshot.trackedFileBackups.backupFileName
```

---

## 9. QUERY UTILI

### Estrarre tutti i messaggi utente

```bash
cat session.jsonl | jq -r 'select(.type == "user") | .message.content'
```

### Estrarre tutti i tool usati

```bash
cat session.jsonl | jq -r '
  select(.type == "assistant")
  | .message.content[]?
  | select(.type == "tool_use")
  | .name
' | sort | uniq -c | sort -rn
```

### Ricostruire timeline

```bash
cat session.jsonl | jq -r '
  select(.timestamp)
  | "\(.timestamp) | \(.type) | \(.message.content[0].type // .message.content[:50])"
'
```

### Trovare errori

```bash
cat session.jsonl | jq 'select(.isApiErrorMessage == true)'
```

---

## 10. EXPORT COMPLETO

Usa lo script `claude_session_extractor.py`:

```bash
# Lista sessioni
python3 claude_session_extractor.py

# Dettagli sessione
python3 claude_session_extractor.py --session <session-id>

# Export completo
python3 claude_session_extractor.py --session <session-id> --export ./backup/

# JSON tutte le relazioni
python3 claude_session_extractor.py --json > all_sessions.json
```

---

## Riferimenti

- Schema completo: `CLAUDE_CODE_SESSION_SCHEMA.md`
- Schema JSON: `claude_code_session_schema.json`
- Extractor: `claude_session_extractor.py`
- Validator: `validate_claude_sessions.py`
