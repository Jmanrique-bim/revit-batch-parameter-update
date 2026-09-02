# HOW_TO: batch update

Purpose: write one replacement string onto the chosen writable text **instance** parameter of every selected element, inside a **single** Revit `Transaction`. Per-element failures become recorded skips; a failed transaction start or a rolled-back commit blocks the session.

## Types

- Port: `src/BatchParamUpdate.Domain/Ports/IParameterWritePort.cs` (`Execute`)
- Adapter: `src/BatchParamUpdate.Adapters.Revit/Writing/RevitParameterWritePort.cs`
- Decision (pure): `src/BatchParamUpdate.Domain/Model/ParameterWriteDecision.cs`
- Suppression: `src/BatchParamUpdate.Adapters.Revit/DialogSuppression/RevitDialogSuppressionPort.cs`
- Worksharing: `src/BatchParamUpdate.Adapters.Revit/Worksharing/RevitWorksharingStatusPort.cs`
- Use case: `src/BatchParamUpdate.Application/UseCases/RunBatchUpdateUseCase.cs`
- Coordinator: `BatchUpdateCoordinator.Run(IProgress<BatchProgress>)`
- Result: `BatchExecutionResult(UpdatedCount, Skips, RolledBack)`

## Gate before write

**Run update** is enabled only when `ReplacementValueViewModel.CanRun` is true: a chosen `WorkflowState.Target`, a non-whitespace value, `SessionState.AwaitingReplacementValue`, and no batch already executing.

`RunBatchUpdateUseCase.Execute` re-checks `operation.HasReplacementValue` (else `ErrorCode.EmptyValue`, no write). Session then `Executing`.

The VM snapshots scope/target/value via `coordinator.PrepareRun()` at click time and passes that `ReplacementOperation` to `coordinator.Run(operation, progress)`; the modeless window defers the write to an ExternalEvent, so the snapshot — not later `State` edits — is what runs.

## The write

`RevitParameterWritePort.Execute` opens one transaction `"Batch Parameter Update"`. For each `ElementRef` it builds a `ParameterState` and hands it to `ParameterWriteDecision.Evaluate(state, trySet)`. The decision order:

| Check fails | `SkipReason` |
|---|---|
| element not found (deleted) | `ElementNotFound` |
| `element.GroupId` set | `ModelGroupMember` |
| checked out by another user | `WorksharingOwnedByOther` |
| parameter not found | `ParameterMissing` |
| parameter read-only | `ParameterReadOnly` |
| not `StorageType.String` | `ParameterNotText` |
| `Parameter.Set` returns `false` (silent Revit reject) | `ValueRejected` |
| otherwise | *updated* |

The target parameter is resolved by `ParameterCandidate.ResolvedKey` — built-in id, then shared GUID, then name — so the write hits the parameter the user picked, not a namesake.

## Commit outcome

- `tx.Commit()` == `Committed` → `BatchExecutionResult.Committed(updated, skips)`; session back to `AwaitingReplacementValue` (another Run allowed).
- `tx.Commit()` != `Committed` → `BatchExecutionResult.Reverted(skips)` → `ErrorCode.BatchRolledBack`; session `Blocked`. The summary reads *"Revit rejected the changes. No elements were modified."* — never a success count — but the per-element skips the run collected are still shown and exportable, and `SessionTraceListener.Aggregate` records no `ok` counts for the run.
- `tx.Start()` fails → port returns `null` → `ErrorCode.DocumentNotModifiable`, session `Blocked`.

## Progress

`Execute` takes `IProgress<BatchProgress>` and reports `(done, total)` per element. The write runs on the Revit API thread (== the UI thread) via `RevitApiEventBridge`; the UI passes `RenderPumpProgress`, which updates `BatchExecutionViewModel.Done/Total` inline and pumps at `Render` priority to repaint without draining input. See `HOW_TO_MVVM.md`.

## Dialog suppression

Wraps the transaction:

1. `UIApplication.DialogBoxShowing` → `OverrideResult(Cancel)`.
2. `IFailuresPreprocessor` deletes warning-severity failure messages and continues.

`SuppressNativeDialogsDuringBatch()` unsubscribes on dispose.

## Summary report

`BatchSummaryViewModel.Show(result, error)` sets the in-window headline and, when there are skips, a searchable/paginated grid (20 rows/page) plus **Export CSV** (`IReportExportPort`, writes to `%USERPROFILE%\Downloads`). One summary, in the same window.
