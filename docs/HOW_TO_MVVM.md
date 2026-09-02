# HOW_TO: MVVM

One WPF window. A `MainViewModel` owns six child view-models; `MainWindow.Bind(MainViewModel)` sets each named control's DataContext.

## Files

- View: `src/BatchParamUpdate.UI.Wpf/Views/MainWindow.xaml` (+ `MainWindow.xaml.cs` `Bind`)
- Child view: `InstanceParameterDialog.xaml` (the single parameter panel, full width)
- Shared styles: `src/BatchParamUpdate.UI.Wpf/Theme.xaml`
- ViewModels: `src/BatchParamUpdate.UI.Wpf/ViewModels/`
- Commands: `RelayCommand` (`ICommand`)

## Who talks to whom

`CompositionRoot` builds the `BatchUpdateCoordinator` and the view-models, then `MainViewModel`. Child view-models **never reference each other** — they read `coordinator.State` (`WorkflowState`: `Scope`, `Target`, `NewValue`) and call coordinator methods. `MainViewModel` is the single subscriber to `coordinator.Changed`; on each change it refreshes the children (`Select.NotifyScopeChanged`, `Discovery.RefreshFromState`, `Replacement.NotifyCanRun`) and re-exposes `ErrorMessage`.

## Bind map

| Control | ViewModel |
|---|---|
| Select Elements + empty banner | `SelectElementsViewModel` |
| Search box | `SharedSearchViewModel` |
| Parameter list, current-values expander | `ParameterDiscoveryViewModel` |
| Advance / block error banner | `MainViewModel` (`ErrorMessage`) |
| Replacement value + Run update | `ReplacementValueViewModel` |
| Progress bar | `BatchExecutionViewModel` (`Done` / `Total`) |
| Summary (headline, paged skip report, export) | `BatchSummaryViewModel` |

View-models implement `INotifyPropertyChanged`. They call the coordinator / Application types; they do not reference RevitAPI. `SelectElementsViewModel` takes `IElementSelectionPort` directly (pick is already the port method); `BatchSummaryViewModel` takes `IReportExportPort` for **Export CSV**.

## Command flow

1. `SelectElementsCommand` → hide window → `PromptManualSelection` → show window → `coordinator.AdoptManualSelection`.
2. Search `Text` → `TextChanged` → `ParameterDiscoveryViewModel.RefreshFilters`; `MainViewModel` records the search via `coordinator.RecordSearch`.
3. Selecting a parameter → `coordinator.ChooseParameter` → `DiscoverParametersUseCase.Choose`. Blocked with `ErrorCode.EmptySelection` if the scope is empty.
4. **Run update** binds `IsEnabled` to `CanRun` (chosen target + non-whitespace value + `AwaitingReplacementValue` + not already executing). `RelayCommand.RaiseCanExecuteChanged` covers the Revit host: `CommandManager` does not requery after `Hide` + `PickObjects` + `Show`.
5. `Run()` is `async void`: sets `IsExecuting`, `await`s `_runOnRevit(() => coordinator.Run(new Progress<BatchProgress>(...)))`, then `BatchSummaryViewModel.Show`. `_runOnRevit` is the `RevitApiEventBridge` in the Revit host (inline default elsewhere).

## Progress bar

`ProgressBar` binds `Value`/`Maximum` (both `OneWay`) to `BatchExecutionViewModel.Done`/`Total`. The window is modeless and the write runs on the Revit API thread via `RevitApiEventBridge`, so the UI thread is free to repaint; `System.Progress<BatchProgress>` marshals `Report` back to it. No dispatcher pump.

## Summary report at scale

`Show(...)` updates the in-window summary only. `_allSkips` holds the full list; the view binds to derived properties: `SearchText` filter, `PagedSkips` (20 rows/page), `PageNumber`/`TotalPages`, `ExportCommand` (writes the full unfiltered list to `%USERPROFILE%\Downloads` via `IReportExportPort`). On a rollback the headline is the `BatchRolledBack` message; the grid still shows the per-element skips the result carries and CSV export still works.

## Constraint

The window is modeless (`Show` on the Revit command; a static `_open` guard refocuses it on a second ribbon click). The batch write must go through `RevitApiEventBridge` (a modeless add-in can only open a `Transaction` from an ExternalEvent callback). `PickObjects` and read-only re-discovery still run as direct calls on the UI/Revit main thread; the window is hidden only for `PickObjects`. `BatchParameterUpdateCommand.Closed` calls `coordinator.Complete()` and disposes the bridge + logger.
