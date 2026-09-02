# HOW_TO: MVVM

One WPF window. Six ViewModels, bound piecewise — not a single root DataContext.

## Files

- View: `src/BatchParamUpdate.UI.Wpf/Views/MainWindow.xaml` (+ `MainWindow.xaml.cs` `Bind`)
- Child views: `InstanceParameterDialog.xaml`, `TypeParameterDialog.xaml`
- Shared styles: `src/BatchParamUpdate.UI.Wpf/Theme.xaml` — brushes and control styles merged by all three views
- ViewModels: `src/BatchParamUpdate.UI.Wpf/ViewModels/`
- Commands: `RelayCommand` (`ICommand`)

`BatchParameterUpdateCommand` constructs the VMs and calls `window.Bind(...)`. Each named control gets its own DataContext.

## Layout

The window is a compact ribbon (UI redesign, "Option 3"): `Select Elements` and the shared-search box live in a top ribbon bar (`IndigoDarkBrush` background), grouped as **Selection** / **Search shared parameters**. There is exactly one `Run update` action — next to the replacement value — no duplicate button in the ribbon. `Theme.xaml` holds the moonlight palette (`#CCCCFF` / `#A3A3CC` / `#5C5C99` / `#292966`) plus white as the dominant surface, reserved for primary actions and the selected/current state; warning/error colors stay semantic and separate from the accent.

## Bind map

| Control | ViewModel |
|---|---|
| Select Elements + empty banner | `SelectElementsViewModel` |
| Search box | `SharedSearchViewModel` |
| Instance/Type lists, current-values expander, advance error | `ParameterDiscoveryViewModel` |
| Replacement value + Run update | `ReplacementValueViewModel` |
| Progress bar | `BatchExecutionViewModel` |
| Summary (headline, paged skip report, export) | `BatchSummaryViewModel` |

ViewModels implement `INotifyPropertyChanged`. They call Application use cases and Domain types; they do not reference RevitAPI. Two ViewModels take a Domain port directly, because the operation is already the port method and there is no extra application policy to wrap: `SelectElementsViewModel` takes `IElementSelectionPort` so pick can run from a command; `BatchSummaryViewModel` takes `IReportExportPort` so **Export CSV** can write the skip list to `%USERPROFILE%\Downloads` without the ViewModel touching the filesystem itself (see `HOW_TO_HEXAGONAL_ARCHITECTURE.md`).

## Command flow

1. `SelectElementsCommand` → hide window → `PromptManualSelection` → show window → raise `Selection`.
2. Search `Text` → `TextChanged` → discovery VM refreshes filtered lists; command records search metrics.
3. Selecting a parameter in either list → `DiscoverParametersUseCase.Choose` → `Operation` / `CurrentValueSummary`. The current-values `Expander` on the replacement panel starts collapsed. `CommandManager.InvalidateRequerySuggested` so **Run update** can enable.
4. `RunCommand` `CanExecute` requires non-whitespace value **and** `SessionState.AwaitingReplacementValue`.
5. `Run()` sets `IsExecuting`, calls `RunBatchUpdateUseCase.Execute` (session returns to `AwaitingReplacementValue` on success), then `BatchSummaryViewModel.Show`.

Type-parameter warning is a bound `Border` (`ShowWideBlastWarning`) wrapping a `TextBlock`, not a modal dialog. `TextBlock` has no `BorderBrush`; the chrome lives on the `Border`.

## Summary report at scale (Report Panel · Variant C)

`Show(...)` updates the in-window summary only — it does not open a second window. The skip list stays in `_allSkips` (from `BatchExecutionResult.InstanceOutcome.Skips`); the view binds to derived properties, never to the raw collection:

- `SearchText` → filters by element label, category, or skip message (case-insensitive); resets to page 1.
- `PagedSkips` → 20 rows (`PageSize`) of the filtered set. The `ItemsControl` in `MainWindow.xaml` only ever renders one page, so a run that skips hundreds of elements never puts hundreds of rows in the visual tree at once.
- `PageNumber` / `TotalPages` / `PageSummary`, driven by `NextPageCommand` / `PreviousPageCommand`.
- `ExportCommand` is enabled only when `HasSkips` is true. It calls `IReportExportPort.ExportSkips(_allSkips, runId)` with the **full, unfiltered** list (written to `%USERPROFILE%\Downloads`). `ExportStatusMessage` reports the written path.

This mirrors the three report options prototyped in the UI redesign (scrollable list, grouped accordion, paginated table + export) — Variant C was the one implemented. The previous popup (`Window` + dump of every skip into a `TextBox`) was removed so the same data is not shown twice.

## Constraint

The window is modal (`ShowDialog` on the Revit command). Keep pick/write on that thread. Hide the window only for `PickObjects`.

Diagram: `docs/diagrams/mvvm-flow.html` (source: `mvvm-flow.workflow.json`) — pre-dates this layout change; regenerate from `mvvm-flow.workflow.json` if kept in sync.
