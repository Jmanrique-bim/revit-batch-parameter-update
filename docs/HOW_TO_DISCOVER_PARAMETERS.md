# HOW_TO: discover parameters

Purpose: from a valid `SelectionContext`, build two deduplicated candidate sets — Instance and Type — of writable **text** parameters, then let the user pick exactly one.

## Types

- Port: `src/BatchParamUpdate.Domain/Ports/IParameterDiscoveryPort.cs`
- Adapter: `src/BatchParamUpdate.Adapters.Revit/Discovery/RevitParameterDiscoveryPort.cs`
- Use case: `src/BatchParamUpdate.Application/UseCases/DiscoverParametersUseCase.cs`
- Sets: `InstanceParameterCandidateSet`, `TypeParameterCandidateSet` (both call `ParameterCandidate.Deduplicate` by name, case-insensitive)
- Search: `SharedSearchQuery` + `SharedSearchViewModel`
- UI: `ParameterDiscoveryViewModel`, `InstanceParameterDialog.xaml`, `TypeParameterDialog.xaml`

## Filter the adapter applies

For each in-scope element:

- **Instance:** walk `element.Parameters`
- **Type:** resolve `element.GetTypeId()` and walk that `ElementType`'s parameters

Keep a parameter only if `StorageType.String`, `!IsReadOnly`, and `Definition.Name` is non-empty. Emit `ParameterCandidate(name, binding, [source ElementRef])`. Domain then unions by name.

Discovery does not write the model.

## Runtime path

1. Command (or a later pick) calls `DiscoverParametersUseCase.Discover(scope)` → both port methods; records phase timing `"Discovery"`.
2. `SharedSearchViewModel.ReplaceSets` holds the full sets. Typing filters both lists with `Name.Contains(text, OrdinalIgnoreCase)`. Empty search text = unfiltered.
3. Search matches are recorded via `RecordSessionUseCase.RecordSearch`.
4. The user selects one list item. Selecting Instance clears Type and vice versa.
5. **Continue** runs `DiscoverParametersUseCase.Choose`. Null candidate → `ErrorCode.NoParameterSelected`. Otherwise session → `AwaitingReplacementValue` and a `ReplacementOperation` with empty `NewValue`.
6. Type selection also sets `ShowWideBlastWarning` (inline, non-modal). Instance does not.

## Constraints

- Both lists are on screen at once; there is no tab to switch binding.
- Candidates are a **union** of names present on at least one in-scope host, not an intersection.
- Type candidates come from the element's type object, not from instance parameters marked as type.
