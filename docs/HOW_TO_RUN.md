# HOW_TO: run the add-in

End-to-end path from Revit load to batch write. `IExternalCommand.Execute` opens a **modeless** WPF window with `Show()` and returns. UI commands, `PickObjects` and read-only re-discovery run on the Revit main/UI thread; the batch write is marshalled to a valid API context through `RevitApiEventBridge` (an `IExternalEventHandler`), because a modeless add-in cannot open a `Transaction` directly.

## Launch

1. Revit loads the year `.addin` (`Application` class `BatchParamUpdate.Adapters.Revit.App`).
2. `App.OnStartup` (`src/BatchParamUpdate.Adapters.Revit/App.cs`) creates ribbon panel **Batch Parameter Update** and push-button **Batch Update**.
3. The button invokes `BatchParameterUpdateCommand` (`src/BatchParamUpdate.Adapters.Revit/ExternalCommand/BatchParameterUpdateCommand.cs`).

The command fails immediately (no window) if there is no `ActiveUIDocument` (`ErrorCode.NoActiveDocument`) or the document is read-only (`ErrorCode.DocumentNotModifiable`).

## Composition

`BatchParameterUpdateCommand` is thin. It:

1. Allocates `runId` (`RunIdGenerator`) and a sanitized document name, opens `SessionFileLogger`.
2. Calls `CompositionRoot.Build(...)` (`src/BatchParamUpdate.Adapters.Revit/Composition/CompositionRoot.cs`) — the single place that references UI + persistence + Revit. It builds the ports, use cases, the `BatchUpdateCoordinator`, the `SessionTraceListener`, and the view-models, and runs `EstablishSelection()`.
3. Creates the `RevitApiEventBridge` (in `CompositionRoot.Build`), sets the window `Owner` to the Revit main window, and calls `Show()`. A static `_open` guard makes a second ribbon click refocus the existing window. The `Closed` handler calls `BatchUpdateCoordinator.Complete()` (resolves the terminal `SessionState`, raises `SessionEnded`) and disposes the bridge + logger.

`BatchUpdateCoordinator` (`src/BatchParamUpdate.Application/Workflow/`) is the only component that advances the `Session` and the only source of `WorkflowEvent`s. `SessionTraceListener` is the only subscriber that writes the log and NDJSON.

## Runtime path in the window

| User action | Who runs | Result |
|---|---|---|
| Open with pre-existing selection | `EstablishSelectionUseCase.DetectPreExisting` → coordinator `Rediscover` | one parameter list populated; session `Discovering` |
| Open with empty selection | coordinator returns `SelectionResult.NeedsManualPick` | window opens, **Select Elements** enabled |
| Pick elements | `SelectElementsViewModel` → `IElementSelectionPort.PromptManualSelection` → `coordinator.AdoptManualSelection` | scope adopted, list re-discovered |
| Type in search | `SharedSearchViewModel` → `ParameterSearch` | live filter of the list |
| Select a parameter | `coordinator.ChooseParameter` → `DiscoverParametersUseCase.Choose` | session `AwaitingReplacementValue`; blocked with `EmptySelection` if scope is empty |
| Run update | `ReplacementValueViewModel` → `coordinator.Run(progress)` → `RunBatchUpdateUseCase` → `IParameterWritePort.Execute` | one transaction; per-element progress; summary (updated / skipped with reasons, or "Revit rejected the changes" on rollback) |

Details: [HOW_TO_SELECTION.md](HOW_TO_SELECTION.md), [HOW_TO_DISCOVER_PARAMETERS.md](HOW_TO_DISCOVER_PARAMETERS.md), [HOW_TO_BATCH_UPDATE.md](HOW_TO_BATCH_UPDATE.md), [HOW_TO_SESSIONS.md](HOW_TO_SESSIONS.md), [HOW_TO_MVVM.md](HOW_TO_MVVM.md).

## Debug vs installer

- **Debug:** build a year project; `Year.props` copies `.addin` + output to `%AppData%\Autodesk\Revit\Addins\{year}`. Restart Revit.
- **Release install:** `src/BatchParamUpdate.Installer/pack.ps1` then `Installer.exe`. Needs Velopack CLI (`vpk`). Installs per-user (`%APPDATA%\Autodesk\Revit\Addins\{year}`), no admin. The installer host calls `VelopackApp.Build().Run()` in `Program.Main` before the WPF window.

## Testing

See [TESTING.md](TESTING.md).
