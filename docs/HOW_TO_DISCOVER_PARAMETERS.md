# HOW_TO: discover parameters

Purpose: from a valid `SelectionContext`, build one deduplicated candidate set of writable **text instance** parameters, then let the user pick exactly one.

## Types

- Port: `src/BatchParamUpdate.Domain/Ports/IParameterDiscoveryPort.cs` (`Discover`)
- Adapter: `src/BatchParamUpdate.Adapters.Revit/Discovery/RevitParameterDiscoveryPort.cs`
- Use case: `src/BatchParamUpdate.Application/UseCases/DiscoverParametersUseCase.cs`
- Set: `ParameterCandidateSet` (calls `ParameterCandidate.Deduplicate`, keyed on `(name, built-in id, shared GUID)` — case-insensitive name — so a namesake with a different Revit identity stays a separate entry)
- Search: `ParameterSearch` + `SharedSearchViewModel`
- UI: `ParameterDiscoveryViewModel`, `InstanceParameterDialog.xaml` (the single parameter panel in `MainWindow.xaml`)

## Filter the adapter applies

For each in-scope element, walk `element.Parameters`. Keep a parameter only if `StorageType.String`, `!IsReadOnly`, and `Definition.Name` is non-empty. Emit `ParameterCandidate(name, [source ElementRef], [AsString], key)` where `key` carries the built-in id / shared GUID. Domain then merges candidates that share `(name, key)`, unioning source refs and distinct observed values.

Discovery does not write the model.

## Runtime path

1. The command (or a later pick) calls `DiscoverParametersUseCase.Discover(scope)` → `IParameterDiscoveryPort.Discover`; records phase timing `"Discovery"`. After a manual pick the command updates `ParameterDiscoveryViewModel` scope (`Retarget`) **before** `ReplaceSet`, so `Choose` never runs against the empty launch context.
2. `SharedSearchViewModel` holds the full set. Typing filters the list with `Name.Contains(text, OrdinalIgnoreCase)`. Empty search text = unfiltered.
3. Search matches are recorded via `RecordSessionUseCase.RecordSearch`.
4. Selecting a list item is the choose step: `DiscoverParametersUseCase.Choose` runs immediately. `Choose` returns a `ReplacementOperation` only after the session is `AwaitingReplacementValue` (`Started` + valid scope, or already `Discovering` / `AwaitingReplacementValue`). Empty scope while `Started` returns null — no current-value line, **Run update** stays disabled. On success the UI shows distinct `ObservedValues` captured at discovery (union: more than one current value is expected).

## Constraints

- Candidates are a **union** of names present on at least one in-scope element, not an intersection. Elements that lack the chosen parameter at execution time are skipped individually and reported.
- Only instance parameters are in scope. Type-bound parameters are not offered.
