# HOW_TO: run the add-in

End-to-end path from Revit load to batch write. All of this runs on the **Revit API thread**: `IExternalCommand.Execute` opens a modal WPF `ShowDialog()`, so UI commands and model writes share that thread.

## Launch

1. Revit loads the year `.addin` (`Application` class `BatchParamUpdate.Adapters.Revit.App`).
2. `App.OnStartup` (`src/BatchParamUpdate.Adapters.Revit/App.cs`) creates ribbon panel **Batch Parameter Update** and push-button **Batch Update**.
3. The button invokes `BatchParameterUpdateCommand` (`src/BatchParamUpdate.Adapters.Revit/ExternalCommand/BatchParameterUpdateCommand.cs`).

The command fails immediately (no window) if there is no `ActiveUIDocument` (`ErrorCode.NoActiveDocument`) or `Document.IsModifiable` is false (`ErrorCode.DocumentNotModifiable`).

## Composition (inside `Execute`)

`BatchParameterUpdateCommand` is the composition root. It:

1. Allocates `runId` (`RunIdGenerator`) and a sanitized document name.
2. Opens `SessionFileLogger` + `NdjsonSessionRecorder` + `RecordSessionUseCase.Start()`.
3. Creates `Session` (`SessionState.Started`).
4. Instantiates Revit adapters and use cases, then ViewModels, then `MainWindow.Bind(...)`.
5. Shows the window; `finally` cancels the session if it is not already Completed/Blocked/Cancelled and calls `RecordSessionUseCase.End`.

## Runtime path in the window

| User action | Who runs | Next |
|---|---|---|
| Open with pre-existing selection | `EstablishSelectionUseCase` + `DiscoverParametersUseCase.Discover` | Instance + Type lists populated |
| Open with empty selection | empty `SelectionContext` (`SelectionOrigin.ManualPick`) | **Select Elements** enabled |
| Pick elements | `SelectElementsViewModel` → `IElementSelectionPort.PromptManualSelection` | rediscover candidates |
| Type in search | `SharedSearchViewModel` | live filter of both lists |
| Continue | `DiscoverParametersUseCase.Choose` | `SessionState.AwaitingReplacementValue` |
| Run update | `ReplacementValueViewModel` → `RunBatchUpdateUseCase.Execute` | write + summary |

Details: [HOW_TO_SELECTION.md](HOW_TO_SELECTION.md), [HOW_TO_DISCOVER_PARAMETERS.md](HOW_TO_DISCOVER_PARAMETERS.md), [HOW_TO_BATCH_UPDATE.md](HOW_TO_BATCH_UPDATE.md), [HOW_TO_SESSIONS.md](HOW_TO_SESSIONS.md), [HOW_TO_MVVM.md](HOW_TO_MVVM.md).

## Debug vs installer

- **Debug:** build a year project; `Year.props` copies `.addin` + output to `%AppData%\Autodesk\REVIT\Addins\{year}`. Restart Revit.
- **Release install:** `src/BatchParamUpdate.Installer/pack.ps1` then `Setup.exe` / `Installer.exe`.

## Diagrams

- `docs/diagrams/session-flow.html` (source: `session-flow.workflow.json`)
- `docs/diagrams/mvvm-flow.html` (source: `mvvm-flow.workflow.json`)

`docs/diagrams/` is gitignored (generated Archify output). Open the HTML locally.
