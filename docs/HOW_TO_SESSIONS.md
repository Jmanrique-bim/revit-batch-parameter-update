# HOW_TO: sessions

Purpose: one command invocation = one `Session` state machine plus two sidecar files (human log + NDJSON metrics). Recording failures must not fail the batch.

## State machine

`src/BatchParamUpdate.Domain/Model/Session.cs` + `SessionState.cs`.

Allowed transitions (terminal states accept nothing):

```
Started → Discovering → AwaitingReplacementValue → Executing → Completed
                                                      Executing → Blocked
any non-terminal → Cancelled
```

Illegal transitions throw. Closing the window without a finished batch is `Cancelled` (`BatchParameterUpdateCommand` `finally`).

Where transitions happen:

- Pre-existing selection / successful pick: `Started` → `Discovering`
- `DiscoverParametersUseCase.Choose`: `Discovering` → `AwaitingReplacementValue`
- `RunBatchUpdateUseCase`: `AwaitingReplacementValue` → `Executing` → `Completed` or `Blocked`

## Recording

`RecordSessionUseCase` (`src/BatchParamUpdate.Application/UseCases/RecordSessionUseCase.cs`) wraps:

- `ILoggerPort` → `Core.SessionFileLogger` (background thread drain to `.txt`)
- `ISessionRecorderPort` → `NdjsonSessionRecorder` (append one JSON object per line)

Identity: `SessionRecord.SessionId` = `revit-{runId}-{documentName}` (`runId` is 8 hex chars). Paths:

- `%TEMP%\juanManriqueHexagon\LOGS\revit-{runId}-{documentName}.txt`
- `%TEMP%\juanManriqueHexagon\TRACKER\revit-{runId}-{documentName}.ndjson`

Events: `SessionStart`, `SearchPerformed`, `ParameterSelected`, `PhaseTiming` (`Discovery` / `Execution`), `BatchResult`, `SessionEnd`.

`End` is idempotent (`_ended`). The use case calls it on Completed/Blocked; the command `finally` also calls it. `SafeRecord` swallows recorder I/O errors and logs `WarningCode.SessionRecordFailed`.

## Diagrams

- `docs/diagrams/session-states.html` (source: `session-states.lifecycle.json`)
- `docs/diagrams/session-flow.html` (source: `session-flow.workflow.json`)
