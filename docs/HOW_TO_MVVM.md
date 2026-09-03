# HOW_TO: MVVM

One WPF window. A `MainViewModel` owns six child view-models; `MainWindow.Bind(MainViewModel)` sets each named control's DataContext.

## Files

- View: `src/BatchParamUpdate.UI.Wpf/Views/MainWindow.xaml` (+ `MainWindow.xaml.cs` `Bind`)
- Child view: `InstanceParameterDialog.xaml` (the single parameter panel, full width)
- Shared styles: `src/BatchParamUpdate.UI.Wpf/Theme.xaml`
- ViewModels: `src/BatchParamUpdate.UI.Wpf/ViewModels/`
- Commands: `RelayCommand` (`ICommand`)

## Who talks to whom

`CompositionRoot` builds the `BatchUpdateCoordinator` and the view-models, then `MainViewModel`. Child view-models **never reference each other** — they read `coordinator.State` (`WorkflowState`: `Scope`, `Target`, `NewValue`) and call coordinator methods. `MainViewModel` is the single subscriber to `coordinator.Changed`; on each change it refreshes the children (`Select.NotifyScopeChanged`, `Replacement.NotifyCanRun`) and re-exposes `ErrorMessage`.

## Bind map

| Control | ViewModel |
|---|---|
| Select Elements + empty banner | `SelectElementsViewModel` |
| Search box | `SharedSearchViewModel` |
| Parameter list | `ParameterDiscoveryViewModel` |
| Advance / block error banner | `MainViewModel` (`ErrorMessage`) |
| Replacement value, current-values expander, Run update | `ReplacementValueViewModel` |
| Progress bar | `BatchExecutionViewModel` (`Done` / `Total`) |
| Summary (headline, paged skip report, export) | `BatchSummaryViewModel` |

View-models implement `INotifyPropertyChanged`. They call the coordinator / Application types; they do not reference RevitAPI. `SelectElementsViewModel` takes `IElementSelectionPort` directly (pick is already the port method); `BatchSummaryViewModel` takes `IReportExportPort` for **Export CSV**.

## Command flow

1. `SelectElementsCommand` → hide window → `PromptManualSelection` → show window → `coordinator.AdoptManualSelection`.
2. Search `Text` → `TextChanged` → `ParameterDiscoveryViewModel.RefreshFilters`; `MainViewModel` records the search via `coordinator.RecordSearch`.
3. Selecting a parameter → `coordinator.ChooseParameter` → `DiscoverParametersUseCase.Choose`. Blocked with `ErrorCode.EmptySelection` if the scope is empty.
4. **Run update** binds `IsEnabled` to `CanRun` (chosen target + non-whitespace value + `AwaitingReplacementValue`). The command is always executable: after Revit 2026 `PickObjects` (Finish/Cancel), `CommandManager` does not requery and a stale `CanExecute=false` would keep the button off.
5. `Run()` sets `IsExecuting`, calls `coordinator.Run(new DispatcherPumpProgress(...))`, then `BatchSummaryViewModel.Show`.

## Progress bar

`ProgressBar` binds `Value`/`Maximum` to `BatchExecutionViewModel.Done`/`Total`. The write runs synchronously on the modal thread, so `DispatcherPumpProgress.Report` drains the WPF queue between elements to keep the bar moving. (Upgrade path in `DispatcherPumpProgress`: modeless window + `IExternalEventHandler`.)

## Summary report at scale

`Show(...)` updates the in-window summary only. `_allSkips` holds the full list; the view binds to derived properties: `SearchText` filter, `PagedSkips` (20 rows/page), `PageNumber`/`TotalPages`, `ExportCommand` (writes the full unfiltered list to `%USERPROFILE%\Downloads` via `IReportExportPort`). On a rollback the grid stays hidden and the headline is the `BatchRolledBack` message.

## Constraint

The window is shown with `Show` + `PushFrame` (not `ShowDialog`). Keep pick/write on that thread; hide the window only for `PickObjects`.
