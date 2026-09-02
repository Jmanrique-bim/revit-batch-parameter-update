# HOW_TO: selection context

Purpose: produce a valid `SelectionContext` (one or more `ElementRef`s plus a `SelectionOrigin`) before discovery and write.

## Types

- Port: `src/BatchParamUpdate.Domain/Ports/IElementSelectionPort.cs`
- Adapter: `src/BatchParamUpdate.Adapters.Revit/Selection/RevitElementSelectionPort.cs`
- Use case: `src/BatchParamUpdate.Application/UseCases/EstablishSelectionUseCase.cs`
- Model: `SelectionContext`, `ElementRef`, `SelectionOrigin` under `src/BatchParamUpdate.Domain/Model/`
- UI: `src/BatchParamUpdate.UI.Wpf/ViewModels/SelectElementsViewModel.cs`

`ElementRef` stores the Revit id as a **string** and the category name. Domain never sees `ElementId`.

## Two origins

`SelectionOrigin.PreExisting` — `UIDocument.Selection.GetElementIds()` at command start.

`SelectionOrigin.ManualPick` — `UIDocument.Selection.PickObjects(ObjectType.Element, ...)`. Cancel throws `OperationCanceledException`; the adapter returns `null`.

`SelectionContext.IsValid` is `ElementRefs.Count > 0`. An empty list is not an error code at this layer; the UI shows the empty-scope banner and blocks useful work until a pick succeeds.

## Who calls whom

`BatchParameterUpdateCommand` does **not** always run `EstablishSelectionUseCase` for a pick:

1. It always asks `GetPreExistingSelection()`.
2. If that context is valid, it runs `EstablishSelectionUseCase.Execute(session)` (same pre-existing read; session → `Discovering`) and discovers immediately.
3. If empty, it builds `new SelectionContext([], SelectionOrigin.ManualPick)` and leaves pick to the UI.

`SelectElementsViewModel.IsSelectElementsEnabled` is true only when `Origin == ManualPick`. Pre-existing scope greys out **Select Elements**. After a successful pick, origin stays `ManualPick`, so the user can pick again; `PropertyChanged` on `Selection` retriggers `DiscoverParametersUseCase.Discover` in the command.

Before `PickObjects`, the command hides `MainWindow` (`beforePick`); after, it shows it again. Revit cannot pick through a modal WPF window.

## Constraints

- `PickObjects` must run on the Revit API thread (it does: modal `ShowDialog` on `Execute`).
- Cancelling the pick leaves the previous context unchanged (still invalid if never picked).
- `EstablishSelectionUseCase` also supports a direct manual prompt when pre-existing is empty; the hosted command currently uses the ViewModel path instead for that case.
