# HOW_TO: selection context

Purpose: produce a valid `SelectionContext` (one or more `ElementRef`s plus a `SelectionOrigin`) before discovery and write.

## Types

- Port: `src/BatchParamUpdate.Domain/Ports/IElementSelectionPort.cs`
- Adapter: `src/BatchParamUpdate.Adapters.Revit/Selection/RevitElementSelectionPort.cs`
- Use case: `src/BatchParamUpdate.Application/UseCases/EstablishSelectionUseCase.cs` (`DetectPreExisting()`)
- Model: `SelectionContext`, `ElementRef`, `SelectionOrigin` under `src/BatchParamUpdate.Domain/Model/`
- Coordinator: `BatchUpdateCoordinator.EstablishSelection()` / `.AdoptManualSelection(...)`
- UI: `src/BatchParamUpdate.UI.Wpf/ViewModels/SelectElementsViewModel.cs`

`ElementRef` stores the Revit id as a **string** and the category name. Domain never sees `ElementId`.

## Two origins

- `SelectionOrigin.PreExisting` — `UIDocument.Selection.GetElementIds()` at command start.
- `SelectionOrigin.ManualPick` — `UIDocument.Selection.PickObjects(ObjectType.Element, ...)`. Cancel throws `OperationCanceledException`; the adapter returns `null`.

`SelectionContext.IsValid` is `ElementRefs.Count > 0`.

## Flow

1. `CompositionRoot` builds the coordinator, then calls `coordinator.EstablishSelection()`.
2. `EstablishSelectionUseCase.DetectPreExisting()` returns whatever the port reports.
   - Valid → `SelectionResult.Established`; the coordinator sets `WorkflowState.Scope`, runs discovery, moves the session to `Discovering`.
   - Empty → `SelectionResult.NeedsManualPick`; the window still opens (User Story 2) with **Select Elements** enabled.
3. `SelectElementsViewModel.PickManually` hides the host window, calls `IElementSelectionPort.PromptManualSelection`, shows it again, then `coordinator.AdoptManualSelection(picked)` — which sets the scope, re-runs discovery, and raises `Changed` so `MainViewModel` refreshes the child view-models. A pick while `SessionState.Executing` is ignored (transaction is open).

`SelectElementsViewModel.IsSelectElementsEnabled` is true only when the launch had no pre-existing selection. Trying to advance (choose a parameter, or Run) while the scope is still empty raises `ErrorCode.EmptySelection`, shown in the error banner.

## Constraints

- `PickObjects` must run on the Revit API thread (it does: `Show` + `PushFrame` on `Execute`). Revit cannot pick through the WPF window, hence the hide/show. Do not use `ShowDialog` — `Hide()` ends that modal loop on Revit 2026.
- The hide/show breaks WPF `CommandManager` requery. **Run update** therefore does not gate `ICommand.CanExecute`; `IsEnabled` binds `CanRun` instead — see `HOW_TO_MVVM.md`.
