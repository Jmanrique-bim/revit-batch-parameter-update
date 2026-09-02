# HOW_TO: MVVM

One WPF window. Six ViewModels, bound piecewise — not a single root DataContext.

## Files

- View: `src/BatchParamUpdate.UI.Wpf/Views/MainWindow.xaml` (+ `MainWindow.xaml.cs` `Bind`)
- Child views: `InstanceParameterDialog.xaml`, `TypeParameterDialog.xaml`
- ViewModels: `src/BatchParamUpdate.UI.Wpf/ViewModels/`
- Commands: `RelayCommand` (`ICommand`)

`BatchParameterUpdateCommand` constructs the VMs and calls `window.Bind(...)`. Each named control gets its own DataContext.

## Bind map

| Control | ViewModel |
|---|---|
| Select Elements + empty banner | `SelectElementsViewModel` |
| Search box | `SharedSearchViewModel` |
| Instance/Type lists, current-value line, advance error | `ParameterDiscoveryViewModel` |
| Replacement value + Run update | `ReplacementValueViewModel` |
| Progress bar | `BatchExecutionViewModel` |
| Summary | `BatchSummaryViewModel` |

ViewModels implement `INotifyPropertyChanged`. They call Application use cases and Domain types; they do not reference RevitAPI. The selection port is passed into `SelectElementsViewModel` so pick can run from a command.

## Command flow

1. `SelectElementsCommand` → hide window → `PromptManualSelection` → show window → raise `Selection`.
2. Search `Text` → `TextChanged` → discovery VM refreshes filtered lists; command records search metrics.
3. Selecting a parameter in either list → `DiscoverParametersUseCase.Choose` → `Operation` / `CurrentValueSummary`. `CommandManager.InvalidateRequerySuggested` so **Run update** can enable.
4. `RunCommand` `CanExecute` requires non-whitespace value **and** `SessionState.AwaitingReplacementValue`.
5. `Run()` sets `IsExecuting`, calls `RunBatchUpdateUseCase.Execute`, then `BatchSummaryViewModel.Show`.

Type-parameter warning is a bound `TextBlock` (`ShowWideBlastWarning`), not a modal dialog.

## Constraint

The window is modal (`ShowDialog` on the Revit command). Keep pick/write on that thread. Hide the window only for `PickObjects`.

Diagram: `docs/diagrams/mvvm-flow.html` (source: `mvvm-flow.workflow.json`).
