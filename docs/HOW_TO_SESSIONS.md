# HOW_TO: sessions

Purpose: one command invocation = one `Session` state machine plus two sidecar files (human log + JSON Lines metrics). Recording failures must not fail the batch — `SessionFileLogger.Write` swallows `IOException` / `UnauthorizedAccessException`.

## State machine

`src/BatchParamUpdate.Domain/Model/Session.cs` + `SessionState.cs`. Only `BatchUpdateCoordinator` and the use cases it owns call `TransitionTo`.

```
Started → Discovering → AwaitingReplacementValue → Executing → AwaitingReplacementValue
                                                     Executing → Blocked
AwaitingReplacementValue → Completed   (window close after at least one committed batch)
any non-terminal → Cancelled
```

Illegal transitions throw. Where they happen:

- `BatchUpdateCoordinator.Rediscover` (pre-existing selection or manual pick): `Started` → `Discovering`.
- `DiscoverParametersUseCase.Choose`: `Discovering` → `AwaitingReplacementValue`.
- `RunBatchUpdateUseCase.Execute`: `AwaitingReplacementValue` → `Executing` → `AwaitingReplacementValue` (committed) or `Blocked` (transaction did not start, or rolled back).
- `BatchUpdateCoordinator.Complete` (window closed): `AwaitingReplacementValue` → `Completed` if a batch actually ran, else `Cancelled` if not already terminal.

## Tracing (events, not inline calls)

The coordinator raises `WorkflowEvent`s (`SessionStarted`, `SelectionEstablished`, `ParametersDiscovered`, `SearchRan`, `ParameterChosen`, `BatchStarting`, `BatchFinished`, `FlowBlocked`, `StateChanged`, `SessionEnded`). On close, `SessionEnded` carries a `Why` snapshot (`empty-scope`, `no-parameter`, `empty-value`, `can-run-never-clicked`, `batch-ran`, `blocked:{code}`) so the last `.txt` lines show why Run never happened. A single `SessionTraceListener` (`src/BatchParamUpdate.Application/Observability/`) is the only subscriber that writes anything:

- `ILoggerPort` → `Core.SessionFileLogger` → `.txt`
- `ISessionRecorderPort` → `Adapters.Persistence.NdjsonSessionRecorder` → one JSON object per line

The flow logic itself contains no logging.

## Files

Identity: `SessionRecord.SessionId` = `revit-{runId}-{documentName}` (`runId` is 8 hex chars). Files on disk use `{runId}-{documentName}`, under a stable per-user root that does **not** follow `TMP`/`TEMP`:

- `%LOCALAPPDATA%\juanManriqueHexagon\LOGS\{runId}-{documentName}.txt`
- `%LOCALAPPDATA%\juanManriqueHexagon\TRACKER\{runId}-{documentName}.json`

Built by `Core.SessionStoragePaths`. NDJSON records: `session_start`, `search_query`, `parameter_selected`, `phase_timing` (`Discovery` / `Execution`), `batch_result` (updated count, skip counts by reason, counts by category and severity), `session_end`.

`SessionTraceListener` swallows recorder I/O errors and logs `WarningCode.SessionRecordFailed`.
