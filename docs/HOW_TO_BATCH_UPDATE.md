# HOW_TO: batch update

Purpose: write one replacement string onto the chosen parameter, Instance or Type, inside a **single** Revit `Transaction`. Per-element failures become skips; a failed transaction start blocks the whole session.

## Types

- Port: `src/BatchParamUpdate.Domain/Ports/IParameterWritePort.cs`
- Adapter: `src/BatchParamUpdate.Adapters.Revit/Writing/RevitParameterWritePort.cs`
- Dialogs: `src/BatchParamUpdate.Adapters.Revit/DialogSuppression/RevitDialogSuppressionPort.cs`
- Use case: `src/BatchParamUpdate.Application/UseCases/RunBatchUpdateUseCase.cs`
- UI: `ReplacementValueViewModel`, `BatchExecutionViewModel`, `BatchSummaryViewModel`
- Result: `BatchExecutionResult` (`InstanceOutcome` or `TypeOutcome`)

## Gate before write

`RunBatchUpdateUseCase.Execute` requires `operation.HasReplacementValue` (non-whitespace). Otherwise `ErrorCode.EmptyValue` and no write. Session then `Executing`.

Binding on `operation.TargetParameter` selects the port method:

- `ExecuteInstanceUpdate`
- `ExecuteTypeUpdate`

If the port returns `null` (transaction did not start), session → `Blocked`, `ErrorCode.DocumentNotModifiable`. On success the session returns to `AwaitingReplacementValue` so another Run is allowed without closing the window.

## Instance path

One transaction named `"Batch Parameter Update"`. For each `ElementRef` in scope, `TryWriteInstance` either `Parameter.Set` or records an `ElementSkip`:

| SkipReason | When |
|---|---|
| `ParameterMissing` | element gone or name not found |
| `ModelGroupMember` | `element.GroupId` is set — Instance path does not write grouped instances |
| `WorksharingOwnedByOther` | `INativeDialogSuppressionPort.GetWorkshareStatus` |
| `ParameterReadOnly` | parameter exists but read-only |
| `ParameterNotText` | not `StorageType.String` |
| `OtherSuppressedNativeDialog` | `InvalidOperationException` from `Set` |

Successful instance writes increment `UpdatedCount`. Summary lists skip messages.

## Type path

Still one transaction. For each in-scope element, resolve `GetTypeId()`, write the **type object** once per distinct type, then count **all** model instances of those types (`FilteredElementCollector`, not only the selection). Group membership is not a Type-path skip: the write is on the shared type.

Summary: `Updated {n} element(s) across {k} type(s).` No per-element skip list on this path.

## Dialog suppression

Wraps the transaction:

1. **Proactive:** worksharing checkout query (no dialog).
2. **Safety net:** `UIApplication.DialogBoxShowing` → `OverrideResult(Cancel)`; `IFailuresPreprocessor` deletes warning-severity failure messages and continues.

`SuppressNativeDialogsDuringBatch()` unsubscribes on dispose. Some native dialogs reject `OverrideResult`; the failures preprocessor is the other layer.

Writes stay on the Revit API thread. The UI shows an indeterminate progress bar while `Run()` is in the `try` (`IsExecuting`).

## Summary report

`BatchSummaryViewModel.Show(...)` updates the in-window headline and, for
the Instance path, a searchable/paginated skip grid (20 rows at a time)
plus an **Export CSV** action that calls `IReportExportPort` directly
(file lands in `%USERPROFILE%\Downloads`) —
see `docs/HOW_TO_MVVM.md` § "Summary report at scale" and
`docs/HOW_TO_HEXAGONAL_ARCHITECTURE.md` for the port. There is no second
summary window. The Type path still has no per-element skip list, so the
grid stays hidden for that outcome.

## Diagram

`docs/diagrams/batch-write.html` (source: `batch-write.sequence.json`).
