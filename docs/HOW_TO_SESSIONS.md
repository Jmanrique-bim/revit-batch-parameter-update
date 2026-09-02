# HOW_TO: sessions

Purpose: one command invocation = one `Session` state machine plus two sidecar files (human log + JSON Lines metrics). Recording failures must not fail the batch.

## State machine

`src/BatchParamUpdate.Domain/Model/Session.cs` + `SessionState.cs`.

Allowed transitions (terminal states accept nothing):

```
Started → Discovering → AwaitingReplacementValue → Executing → AwaitingReplacementValue
                                                      Executing → Blocked
AwaitingReplacementValue → Completed   (window close after at least one batch)
any non-terminal → Cancelled
```

Illegal transitions throw. Closing the window without a finished batch is `Cancelled`. Closing after at least one successful batch is `Completed` (`BatchParameterUpdateCommand` `finally`).

Where transitions happen:

- Pre-existing selection / successful pick: `Started` → `Discovering`
- `DiscoverParametersUseCase.Choose`: `Started` + valid scope → `Discovering` → `AwaitingReplacementValue`, or `Discovering` → `AwaitingReplacementValue`. Already `AwaitingReplacementValue` stays there (re-pick a parameter). If the session never reaches `AwaitingReplacementValue` (empty scope while `Started`), `Choose` returns null and does not emit an operation.
- `RunBatchUpdateUseCase`: `AwaitingReplacementValue` → `Executing` → `AwaitingReplacementValue` (success, so another Run is allowed) or `Blocked` (transaction did not start)
- Command `finally`: `AwaitingReplacementValue` → `Completed` when `RecordSessionUseCase.HasBatch`; otherwise Cancelled if not already terminal

`End` is idempotent (`_ended`). The use case calls it only on Blocked. The command `finally` always calls it, and is the only place a successful session becomes `Completed`.

## Recording

`RecordSessionUseCase` (`src/BatchParamUpdate.Application/UseCases/RecordSessionUseCase.cs`) wraps:

- `ILoggerPort` → `Core.SessionFileLogger` (background thread drain to `.txt`)
- `ISessionRecorderPort` → `NdjsonSessionRecorder` (append one JSON object per line)

Identity: `SessionRecord.SessionId` = `revit-{runId}-{documentName}` (`runId` is 8 hex chars). Files on disk use `{runId}-{documentName}` (no `revit-` prefix), under a stable per-user root that does **not** follow `TMP`/`TEMP`:

- `%LOCALAPPDATA%\juanManriqueHexagon\LOGS\{runId}-{documentName}.txt`
- `%LOCALAPPDATA%\juanManriqueHexagon\TRACKER\{runId}-{documentName}.json`

Paths are built by `Core.SessionStoragePaths`. Tracker content is still JSON Lines (one object per `AppendAllText`).

Events: `SessionStart`, `SearchPerformed`, `ParameterSelected`, `PhaseTiming` (`Discovery` / `Execution`), `BatchResult`, `SessionEnd`.

`SafeRecord` swallows recorder I/O errors and logs `WarningCode.SessionRecordFailed`.

## Diagrams

- `docs/diagrams/session-states.html` (source: `session-states.lifecycle.json`)
- `docs/diagrams/session-flow.html` (source: `session-flow.workflow.json`)
