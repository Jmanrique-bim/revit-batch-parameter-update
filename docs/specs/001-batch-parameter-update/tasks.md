# Tasks: Batch Parameter Update Revit Add-in

**Input**: Design documents in `docs/specs/001-batch-parameter-update/`

**Prerequisites**: `plan.md` (required), `spec.md` (required, user stories), `research.md`, `data-model.md`, `contracts/ports.md`, `quickstart.md`, `.specify/memory/constitution.md`

**Tests**: The spec explicitly mandates Testability as one of the 4 pillars (Assumptions & Mandated Constraints → "Four delivery pillars"), and `research.md` §g / `plan.md` decide xUnit + hand-written fakes as the testing strategy for `Domain`/`Application` decoupled from Revit. Therefore **yes, test tasks are generated** for the `Domain`/`Application`/`Core` layers in each user story — this is an explicit spec/plan requirement, not gratuitous TDD.

**Organization**: Tasks are grouped by user story (US0 ribbon launch, then US1–US5 of `spec.md`) to allow independent implementation and test. Each task is atomic and scoped to 1 file (or a minimal cohesive group of closely related files), so work can proceed in parallel within each story whenever the dependency graph allows.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (distinct file, no dependency on an incomplete task)
- **[Story]**: Which user story the task belongs to (US0–US5); Setup/Foundational/Polish have no story label
- Each description includes the exact file path

## Path Conventions

Desktop project (Revit add-in + standalone installer), per `plan.md` → Project Structure:

```text
src/BatchParamUpdate.Core/                  # Cross-cutting: SessionFileLogger, runId, name sanitizing
src/BatchParamUpdate.Domain/                #   Model/, Ports/, ErrorCatalog/ — ZERO RevitAPI.dll reference
src/BatchParamUpdate.Application/           #   UseCases/ — orchestrates Domain + ports, no RevitAPI.dll
src/BatchParamUpdate.Adapters.Revit/        #   App.cs, Resources/, Selection/, Discovery/, Writing/, DialogSuppression/, ExternalCommand/
src/BatchParamUpdate.Adapters.Persistence/  #   NDJSON + .txt under %TEMP%\juanManriqueHexagon
src/BatchParamUpdate.UI.Wpf/                #   ViewModels/, Views/
src/BatchParamUpdate.Installer/             #   WPF host packaged with Velopack
tests/BatchParamUpdate.Tests.Unit/          #   Domain/, Application/, Fakes/
```

**Repo status note**: `src/` (except copied `Resources/` icons) , `tests/`, and the `.sln` do not exist yet — Phase 1 creates the real scaffolding from scratch.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Initialize the solution and the 8 hexagonal-layer projects (`plan.md` → Project Structure), including conditional per-Revit-year multi-targeting (FR-048), plus ribbon/`App` bootstrap so `IExternalApplication` exists before `IExternalCommand` (FR-049–FR-051, US0).

- [ ] T001 Create solution file `BatchParamUpdate.sln` at the repository root
- [ ] T002 [P] Create Class Library project `BatchParamUpdate.Domain` (`net8.0`, no `-windows`) at `src/BatchParamUpdate.Domain/BatchParamUpdate.Domain.csproj`
- [ ] T003 [P] Create Class Library project `BatchParamUpdate.Core` (`net8.0-windows`) at `src/BatchParamUpdate.Core/BatchParamUpdate.Core.csproj`
- [ ] T004 Create Class Library project `BatchParamUpdate.Application` (`net8.0`) at `src/BatchParamUpdate.Application/BatchParamUpdate.Application.csproj`, referencing `Domain` (depends on T002)
- [ ] T005 Create project `BatchParamUpdate.Adapters.Persistence` (`net8.0-windows`) at `src/BatchParamUpdate.Adapters.Persistence/BatchParamUpdate.Adapters.Persistence.csproj`, referencing `Domain` and `Core` (depends on T002, T003)
- [ ] T006 Create project `BatchParamUpdate.Adapters.Revit` (initial TFM `net8.0-windows`) at `src/BatchParamUpdate.Adapters.Revit/BatchParamUpdate.Adapters.Revit.csproj`, referencing `Domain` (depends on T002)
- [ ] T007 Configure multi-targeting `<TargetFrameworks>net8.0-windows;net10.0-windows</TargetFrameworks>` and the 6 per-year build configurations (`Debug2025`/`Release2025`, `Debug2026`/`Release2026`, `Debug2027`/`Release2027`) with property `RevitVersion` in `BatchParamUpdate.Adapters.Revit.csproj` (research.md §a, FR-048) (depends on T006)
- [ ] T008 Configure conditional references to `RevitAPI.dll`/`RevitAPIUI.dll` by `RevitVersion` (variable `HintPath`, `Private=false`, `CopyLocal=false`) and `DefineConstants` symbols (`REVIT2025_OR_GREATER`, `REVIT2026_OR_GREATER`, `REVIT2027_OR_GREATER`) in `BatchParamUpdate.Adapters.Revit.csproj` (research.md §a) (depends on T007)
- [ ] T009 [P] Create WPF project `BatchParamUpdate.UI.Wpf` (`net8.0-windows`) at `src/BatchParamUpdate.UI.Wpf/BatchParamUpdate.UI.Wpf.csproj`, referencing `Application` and `Domain` (depends on T002, T004)
- [ ] T010 [P] Create WPF project `BatchParamUpdate.Installer` (`net8.0-windows`) at `src/BatchParamUpdate.Installer/BatchParamUpdate.Installer.csproj`
- [ ] T011 Create xUnit test project `BatchParamUpdate.Tests.Unit` at `tests/BatchParamUpdate.Tests.Unit/BatchParamUpdate.Tests.Unit.csproj`, referencing only `Domain` and `Application` (research.md §g) (depends on T002, T004)
- [ ] T012 Add the 8 projects (`Domain`, `Core`, `Application`, `Adapters.Persistence`, `Adapters.Revit`, `UI.Wpf`, `Installer`, `Tests.Unit`) to `BatchParamUpdate.sln` (depends on T002–T011)
- [ ] T013 [P] Add `Directory.Build.props` at the root with shared `Nullable=enable`, `ImplicitUsings=enable`, and `LangVersion` for all projects
- [ ] T014 [P] Add `.editorconfig` at the root with the project's C# style rules

### Ribbon & Application bootstrap (US0)

- [ ] T015 [P] Copy the assignment lineal-color optimization icons from `C:\Users\Juan -- IP\Descargas\icons8-optimization-lineal-color` (`icons8-optimization-64.png`, `icons8-optimization-100.png`) into `src/BatchParamUpdate.Adapters.Revit/Resources/` and include them as Content/EmbeddedResource in `BatchParamUpdate.Adapters.Revit.csproj` (FR-051, research.md §i) (depends on T006)
- [ ] T016 Implement `App` as `IExternalApplication` with `OnStartup`/`OnShutdown` at `src/BatchParamUpdate.Adapters.Revit/App.cs` (FR-049, research.md §i) (depends on T008)
- [ ] T017 Create a custom `RibbonPanel` and a dedicated `PushButton` in `App.OnStartup` targeting `BatchParameterUpdateCommand` (not a generic Add-Ins tab dump) at `src/BatchParamUpdate.Adapters.Revit/App.cs` (FR-049/FR-050) (depends on T016)
- [ ] T018 Point the `.addin` manifest `Application` class at `App` (`IExternalApplication`), not command-only registration, at `src/BatchParamUpdate.Adapters.Revit/ExternalCommand/BatchParamUpdate.addin` (FR-049, research.md §i) (depends on T016)
- [ ] T019 Wire small/large ribbon images on the `PushButton` (16px `Image` / 32px `LargeImage`, derived at implementation from the 64/100 PNGs if needed) from `src/BatchParamUpdate.Adapters.Revit/Resources/` in `src/BatchParamUpdate.Adapters.Revit/App.cs` (FR-051, research.md §i) (depends on T015, T017)

**Checkpoint**: Compilable solution with the 8 empty projects referencing each other correctly, plus `App` ribbon bootstrap and icon resources in place.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: `Domain`/`Core` entities, ports, and utilities consumed by more than one user story (session identity, 400/500 catalog, logging) — block the start of any story.

**⚠️ CRITICAL**: No user story (other than the Setup ribbon bootstrap already above) can start until this phase is complete.

- [ ] T020 [P] Create record `ElementRef` (`src/BatchParamUpdate.Domain/Model/ElementRef.cs`) with opaque string identifier and `CategoryName` (data-model.md §1)
- [ ] T021 [P] Create enum `ParameterBinding` (`src/BatchParamUpdate.Domain/Model/ParameterBinding.cs`: `Instance`, `Type`)
- [ ] T022 [P] Create enum `SessionState` (`src/BatchParamUpdate.Domain/Model/SessionState.cs`: `Started`, `Discovering`, `AwaitingReplacementValue`, `Executing`, `Completed`, `Blocked`, `Cancelled`)
- [ ] T023 Create class `Session` with state machine and transition invariants (`src/BatchParamUpdate.Domain/Model/Session.cs`, data-model.md §9) (depends on T022)
- [ ] T024 [P] Create enum `ErrorCode` (`src/BatchParamUpdate.Domain/ErrorCatalog/ErrorCode.cs`) with the 5 codes 500 from data-model.md §7 (`EMPTY-SELECTION`, `NO-PARAMETER-SELECTED`, `EMPTY-VALUE`, `DOCUMENT-NOT-MODIFIABLE`, `NO-ACTIVE-DOCUMENT`)
- [ ] T025 [P] Create enum `WarningCode` (`src/BatchParamUpdate.Domain/ErrorCatalog/WarningCode.cs`) with the 7 codes 400 from data-model.md §7 (`PARAM-MISSING`, `PARAM-READONLY`, `PARAM-NOT-TEXT`, `WORKSHARE-OWNED`, `MODEL-GROUP-MEMBER`, `NO-SEARCH-MATCH`, `SESSION-RECORD-FAILED`)
- [ ] T026 Create static class `ErrorWarningCatalog` mapping each `ErrorCode`/`WarningCode` to its severity (400/500) and the literal non-technical message from data-model.md §7 (`src/BatchParamUpdate.Domain/ErrorCatalog/ErrorWarningCatalog.cs`, FR-027/FR-028) (depends on T024, T025)
- [ ] T027 [P] Create record `SessionRecord` (`src/BatchParamUpdate.Domain/Model/SessionRecord.cs`: `RunId`, `DocumentName`, `SessionId`, `StartedAtUtc`, FR-038)
- [ ] T028 [P] Create interface `ILoggerPort` (`src/BatchParamUpdate.Domain/Ports/ILoggerPort.cs`: `Info`, `Warn`, `Error`, `CloseSession`, contracts/ports.md §5)
- [ ] T029 [P] Create interface `ISessionRecorderPort` (`src/BatchParamUpdate.Domain/Ports/ISessionRecorderPort.cs`: `Record(MetricsRecord)`, contracts/ports.md §6)
- [ ] T030 Create the `MetricsRecord` type hierarchy (`src/BatchParamUpdate.Domain/Model/MetricsRecord.cs`: `SessionStart`, `SearchPerformed`, `ParameterSelected`, `PhaseTiming`, `BatchResult`, `SessionEnd`, data-model.md §8) (depends on T027)
- [ ] T031 [P] Create utility `RunIdGenerator` (`src/BatchParamUpdate.Core/RunIdGenerator.cs`) that generates an 8-character hex GUID per session (research.md §f, FR-034)
- [ ] T032 [P] Create utility `DocumentNameSanitizer` (`src/BatchParamUpdate.Core/DocumentNameSanitizer.cs`) that sanitizes `Document.Title` to a valid NTFS file name, truncated to 60 characters (research.md §f)
- [ ] T033 Implement `SessionFileLogger` as the canonical `ILoggerPort` implementation, with an in-memory queue (`BlockingCollection<string>`) drained by a background thread to the session `.txt` (`src/BatchParamUpdate.Core/SessionFileLogger.cs`, research.md §e) (depends on T028, T031, T032)
- [ ] T034 [P] Create in-memory fake of `ILoggerPort` for tests (`tests/BatchParamUpdate.Tests.Unit/Fakes/FakeLoggerPort.cs`) (depends on T028)
- [ ] T035 [P] Create in-memory fake of `ISessionRecorderPort` for tests (`tests/BatchParamUpdate.Tests.Unit/Fakes/FakeSessionRecorderPort.cs`) (depends on T029)
- [ ] T036 [P] Unit test: valid and invalid `Session` state-machine transitions (`tests/BatchParamUpdate.Tests.Unit/Domain/SessionTests.cs`, data-model.md §9) (depends on T023)
- [ ] T037 [P] Unit test: each catalog `ErrorCode`/`WarningCode` has exactly one severity and one non-technical message (`tests/BatchParamUpdate.Tests.Unit/Domain/ErrorWarningCatalogTests.cs`) (depends on T026)

**Checkpoint**: Foundation ready — user-story implementation can start.

---

## Phase 3: User Story 1 - Recognize a pre-existing selection at add-in launch (Priority: P1) 🎯 MVP

**Goal**: When launching the command (from the dedicated ribbon button) with elements already selected in Revit, the add-in adopts that selection as session scope without asking for reselection, and the "Select Elements" control is disabled (FR-001–FR-003).

**Independent Test**: Select a mixed set of elements in Revit, launch from the dedicated ribbon button, and verify the add-in recognizes the existing selection (no reselection) and proceeds to parameter discovery.

### Tests for User Story 1

- [ ] T038 [P] [US1] Unit test: `SelectionContext.IsValid` is `false` when `ElementRefs` is empty (`tests/BatchParamUpdate.Tests.Unit/Domain/SelectionContextTests.cs`, FR-006)
- [ ] T039 [US1] Unit test: `EstablishSelectionUseCase` adopts the pre-existing selection without invoking the manual pick when `GetPreExistingSelection` returns elements (`tests/BatchParamUpdate.Tests.Unit/Application/EstablishSelectionUseCaseTests.cs`, FR-001/FR-002)

### Implementation for User Story 1

- [ ] T040 [P] [US1] Create enum `SelectionOrigin` (`src/BatchParamUpdate.Domain/Model/SelectionOrigin.cs`: `PreExisting`, `ManualPick`)
- [ ] T041 [US1] Create entity `SelectionContext` with `IsValid` invariant (`src/BatchParamUpdate.Domain/Model/SelectionContext.cs`, data-model.md §1) (depends on T020, T040)
- [ ] T042 [US1] Create interface `IElementSelectionPort` with `GetPreExistingSelection`/`PromptManualSelection` (`src/BatchParamUpdate.Domain/Ports/IElementSelectionPort.cs`, contracts/ports.md §1) (depends on T041)
- [ ] T043 [P] [US1] Create in-memory fake of `IElementSelectionPort` for tests (`tests/BatchParamUpdate.Tests.Unit/Fakes/FakeElementSelectionPort.cs`) (depends on T042)
- [ ] T044 [US1] Create `EstablishSelectionUseCase` in `Application` that resolves `GetPreExistingSelection` at session start (`src/BatchParamUpdate.Application/UseCases/EstablishSelectionUseCase.cs`, FR-001/FR-002) (depends on T042, T023)
- [ ] T045 [US1] Implement `RevitElementSelectionPort.GetPreExistingSelection` with `UIDocument.Selection.GetElementIds()` (`src/BatchParamUpdate.Adapters.Revit/Selection/RevitElementSelectionPort.cs`) (depends on T042)
- [ ] T046 [P] [US1] Create `SelectElementsViewModel` with property `IsSelectElementsEnabled` derived from `SelectionOrigin` (`src/BatchParamUpdate.UI.Wpf/ViewModels/SelectElementsViewModel.cs`, FR-003) (depends on T041)
- [ ] T047 [P] [US1] Bind the "Select Elements" control to `IsSelectElementsEnabled` on the main window, disabled when `Origin=PreExisting` (`src/BatchParamUpdate.UI.Wpf/Views/MainWindow.xaml`) (depends on T046)

**Checkpoint**: US1 functional and independently testable.

---

## Phase 4: User Story 2 - Select elements manually from inside the add-in (Priority: P1)

**Goal**: When launching the add-in with no prior selection, the "Select Elements" control is enabled and lets the user pick elements from inside the model (FR-004–FR-006).

**Independent Test**: Launch the add-in from the dedicated ribbon button with an empty Revit selection and verify the "Select Elements" control enables and, after use, populates the scope with the picked elements.

### Tests for User Story 2

- [ ] T048 [P] [US2] Unit test: `SelectionContext` with `Origin=ManualPick` leaves the "Select Elements" control enabled (`tests/BatchParamUpdate.Tests.Unit/Domain/SelectionContextTests.cs`, FR-004)
- [ ] T049 [US2] Unit test: `EstablishSelectionUseCase` invokes `PromptManualSelection` when no pre-existing selection exists (`tests/BatchParamUpdate.Tests.Unit/Application/EstablishSelectionUseCaseTests.cs`, FR-005)
- [ ] T050 [US2] Unit test: `EstablishSelectionUseCase` leaves the session without a valid scope when the user cancels the manual pick (`PromptManualSelection` returns `null`) (`tests/BatchParamUpdate.Tests.Unit/Application/EstablishSelectionUseCaseTests.cs`, FR-006, US2 scenario 3)

### Implementation for User Story 2

- [ ] T051 [US2] Extend `EstablishSelectionUseCase` to invoke `PromptManualSelection` when there is no pre-existing selection and to handle cancellation (`src/BatchParamUpdate.Application/UseCases/EstablishSelectionUseCase.cs`) (depends on T044)
- [ ] T052 [US2] Implement `RevitElementSelectionPort.PromptManualSelection` with `UIDocument.Selection.PickObjects(...)`, returning `null` if the user cancels (`src/BatchParamUpdate.Adapters.Revit/Selection/RevitElementSelectionPort.cs`) (depends on T045)
- [ ] T053 [P] [US2] Add a "Select Elements" command to `SelectElementsViewModel` that invokes the manual pick (`src/BatchParamUpdate.UI.Wpf/ViewModels/SelectElementsViewModel.cs`) (depends on T046)
- [ ] T054 [P] [US2] Add a "no elements in scope" indicator to the main window when `SelectionContext.IsValid` is `false` (`src/BatchParamUpdate.UI.Wpf/Views/MainWindow.xaml`) (depends on T047)

**Checkpoint**: US1 and US2 work independently, covering the two selection flows mandated by the stakeholder.

---

## Phase 5: User Story 3 - Discover and choose the target parameter from two simultaneous jointly-searched dialogs (Priority: P1)

**Goal**: Dialog Box 1 (Instance) and Dialog Box 2 (Type) are shown together over the same scope, with deduplicated-union discovery and a single search that filters both lists live (FR-007–FR-014).

**Independent Test**: Open the add-in against a selection with varied Instance and Type text parameters; confirm both dialogs are visible at once, that one search filters both live, that each parameter appears once per binding, and that the flow does not advance without exactly one chosen parameter.

### Tests for User Story 3

- [ ] T055 [P] [US3] Unit test: a `ParameterCandidate.Name` appears once per `Binding` inside its set (deduplication) (`tests/BatchParamUpdate.Tests.Unit/Domain/ParameterCandidateSetTests.cs`, FR-007/FR-009)
- [ ] T056 [P] [US3] Unit test: `SharedSearchQuery` filters both sets simultaneously by case-insensitive substring on `Definition.Name` (`tests/BatchParamUpdate.Tests.Unit/Domain/SharedSearchQueryTests.cs`, FR-011)
- [ ] T057 [US3] Unit test: `DiscoverParametersUseCase` blocks advancing to the replacement step when no `TargetParameter` is chosen (`ERR-500-NO-PARAMETER-SELECTED`) (`tests/BatchParamUpdate.Tests.Unit/Application/DiscoverParametersUseCaseTests.cs`, FR-013)
- [ ] T058 [US3] Unit test: choosing a candidate from `TypeParameterCandidateSet` sets `RequiresWideBlastRadiusWarning=true` without blocking advance (`tests/BatchParamUpdate.Tests.Unit/Application/DiscoverParametersUseCaseTests.cs`, FR-014, SC-010)

### Implementation for User Story 3

- [ ] T059 [P] [US3] Create shared record `ParameterCandidate` (`src/BatchParamUpdate.Domain/Model/ParameterCandidate.cs`: `Name`, `Binding`, `SourceElementRefs`, data-model.md §2) (depends on T020, T021)
- [ ] T060 [P] [US3] Create `InstanceParameterCandidateSet` with `Name` deduplication invariant (`src/BatchParamUpdate.Domain/Model/InstanceParameterCandidateSet.cs`, FR-007) (depends on T059)
- [ ] T061 [P] [US3] Create `TypeParameterCandidateSet` with `Name` deduplication invariant (`src/BatchParamUpdate.Domain/Model/TypeParameterCandidateSet.cs`, FR-008) (depends on T059)
- [ ] T062 [US3] Create `SharedSearchQuery` with in-memory filtering of `MatchesInstanceSet`/`MatchesTypeSet` (`src/BatchParamUpdate.Domain/Model/SharedSearchQuery.cs`, FR-011/FR-012) (depends on T060, T061)
- [ ] T063 [US3] Create interface `IParameterDiscoveryPort` with `DiscoverInstanceCandidates`/`DiscoverTypeCandidates` (`src/BatchParamUpdate.Domain/Ports/IParameterDiscoveryPort.cs`, contracts/ports.md §2) (depends on T060, T061)
- [ ] T064 [P] [US3] Create in-memory fake of `IParameterDiscoveryPort` for tests (`tests/BatchParamUpdate.Tests.Unit/Fakes/FakeParameterDiscoveryPort.cs`) (depends on T063)
- [ ] T065 [P] [US3] Create record `ResolvedType` and discriminated `ExecutionScope` (`InstanceScope`/`TypeScope`) (`src/BatchParamUpdate.Domain/Model/ExecutionScope.cs`, data-model.md §5) (depends on T020)
- [ ] T066 [US3] Create entity `ReplacementOperation` with `TargetParameter`/`NewValue`/`RequiresWideBlastRadiusWarning`/`ExecutionScope` (`src/BatchParamUpdate.Domain/Model/ReplacementOperation.cs`, FR-013/FR-014) (depends on T059, T065)
- [ ] T067 [US3] Create `DiscoverParametersUseCase` in `Application` that orchestrates discovery, applies the shared search, and validates exactly one parameter selection (`src/BatchParamUpdate.Application/UseCases/DiscoverParametersUseCase.cs`) (depends on T062, T063, T066, T023)
- [ ] T068 [US3] Implement `RevitParameterDiscoveryPort.DiscoverInstanceCandidates` iterating `Element.Parameters` with filter `StorageType.String && !IsReadOnly` (`src/BatchParamUpdate.Adapters.Revit/Discovery/RevitParameterDiscoveryPort.cs`, research.md §d) (depends on T063)
- [ ] T069 [US3] Implement `RevitParameterDiscoveryPort.DiscoverTypeCandidates` resolving `document.GetElement(element.GetTypeId()).Parameters` (`src/BatchParamUpdate.Adapters.Revit/Discovery/RevitParameterDiscoveryPort.cs`) (depends on T068)
- [ ] T070 [P] [US3] Create `ParameterDiscoveryViewModel` with the filtered Dialog Box 1/2 lists and the parameter-selection command (`src/BatchParamUpdate.UI.Wpf/ViewModels/ParameterDiscoveryViewModel.cs`, FR-010/FR-013/FR-014) (depends on T067)
- [ ] T071 [P] [US3] Create `SharedSearchViewModel` that updates `SharedSearchQuery.Text` live as the user types (`src/BatchParamUpdate.UI.Wpf/ViewModels/SharedSearchViewModel.cs`, FR-011) (depends on T062)
- [ ] T072 [P] [US3] Create Dialog Box 1 (Instance) view with candidate list and "no results" message (`src/BatchParamUpdate.UI.Wpf/Views/InstanceParameterDialog.xaml`, FR-012) (depends on T070)
- [ ] T073 [P] [US3] Create Dialog Box 2 (Type) view with candidate list, "no results" message, and the non-blocking inline warning when selecting a candidate (`src/BatchParamUpdate.UI.Wpf/Views/TypeParameterDialog.xaml`, FR-012/FR-014) (depends on T070)

**Checkpoint**: US1, US2, and US3 work independently — the user can get as far as having a chosen parameter.

---

## Phase 6: User Story 4 - Enter the replacement value and run the batch update (Priority: P1)

**Goal**: Execute the Instance or Type update inside a single reversible transaction, with inline progress, automatic native-dialog suppression, proactive skip of Model Group elements, and a classified final summary (FR-015–FR-026).

**Independent Test — Instance path**: Select a mix of elements (parameter present/missing/read-only, one workshared owned-by-other, one Model Group), run, and confirm updates only where valid, with no native dialog, inline progress, and a summary with skip reasons. **Independent Test — Type path**: choose a Type parameter and confirm every model element of that type (including Model Group members) reflects the new value.

### Tests for User Story 4

- [ ] T074 [P] [US4] Unit test: `RunBatchUpdateUseCase` rejects empty or blank `NewValue` with `ERR-500-EMPTY-VALUE` (`tests/BatchParamUpdate.Tests.Unit/Application/RunBatchUpdateUseCaseTests.cs`, FR-016)
- [ ] T075 [US4] Unit test: `ExecuteInstanceUpdate` produces an `ElementSkip` for each `SkipReason` (`ParameterMissing`, `ParameterReadOnly`, `ParameterNotText`, `WorksharingOwnedByOther`, `ModelGroupMember`) according to the configured fake (`tests/BatchParamUpdate.Tests.Unit/Application/RunBatchUpdateUseCaseTests.cs`, FR-020/FR-024/FR-025)
- [ ] T076 [US4] Unit test: `ExecuteTypeUpdate` returns a `TypeOutcome` with `AffectedTypes` and model-wide `TotalElementsUpdated` (`tests/BatchParamUpdate.Tests.Unit/Application/RunBatchUpdateUseCaseTests.cs`, FR-018)
- [ ] T077 [P] [US4] Unit test: `BatchExecutionResult.InstanceOutcome` and `TypeOutcome` are mutually exclusive according to `Path` (`tests/BatchParamUpdate.Tests.Unit/Domain/BatchExecutionResultTests.cs`, FR-017/FR-018)
- [ ] T078 [P] [US4] Unit test: a globally blocked operation produces no `BatchExecutionResult` and no element is considered modified (`tests/BatchParamUpdate.Tests.Unit/Application/RunBatchUpdateUseCaseTests.cs`, FR-019, SC-005)

### Implementation for User Story 4

- [ ] T079 [P] [US4] Create enum `SkipReason` (`src/BatchParamUpdate.Domain/Model/SkipReason.cs`: `ParameterMissing`, `ParameterReadOnly`, `ParameterNotText`, `WorksharingOwnedByOther`, `ModelGroupMember`, `OtherSuppressedNativeDialog`)
- [ ] T080 [P] [US4] Create enum `WorkshareStatus` (`src/BatchParamUpdate.Domain/Model/WorkshareStatus.cs`: `NotWorkshared`, `OwnedByCurrentUser`, `OwnedByOtherUser`)
- [ ] T081 [US4] Create record `ElementSkip` (`src/BatchParamUpdate.Domain/Model/ElementSkip.cs`: `Element`, `Reason`, `Code`, `Message`, FR-021) (depends on T020, T025, T079)
- [ ] T082 [US4] Create entity `BatchExecutionResult` with mutually exclusive `InstanceOutcome`/`TypeOutcome` according to `Path` (`src/BatchParamUpdate.Domain/Model/BatchExecutionResult.cs`, data-model.md §6, FR-026) (depends on T081, T065)
- [ ] T083 [US4] Extend `ReplacementOperation` with empty/blank `NewValue` validation at execution (`src/BatchParamUpdate.Domain/Model/ReplacementOperation.cs`, FR-016) (depends on T066)
- [ ] T084 [US4] Create interface `IParameterWritePort` with `ExecuteInstanceUpdate`/`ExecuteTypeUpdate` (`src/BatchParamUpdate.Domain/Ports/IParameterWritePort.cs`, contracts/ports.md §3) (depends on T082)
- [ ] T085 [US4] Create interface `INativeDialogSuppressionPort` with `GetWorkshareStatus`/`SuppressNativeDialogsDuringBatch` (`src/BatchParamUpdate.Domain/Ports/INativeDialogSuppressionPort.cs`, contracts/ports.md §4) (depends on T080)
- [ ] T086 [P] [US4] Create in-memory fake of `IParameterWritePort` for tests (`tests/BatchParamUpdate.Tests.Unit/Fakes/FakeParameterWritePort.cs`) (depends on T084)
- [ ] T087 [P] [US4] Create in-memory fake of `INativeDialogSuppressionPort` for tests (`tests/BatchParamUpdate.Tests.Unit/Fakes/FakeNativeDialogSuppressionPort.cs`) (depends on T085)
- [ ] T088 [US4] Create `RunBatchUpdateUseCase` in `Application` that validates `NewValue`, executes the Instance or Type path according to `TargetParameter.Binding`, and produces the `BatchExecutionResult` (`src/BatchParamUpdate.Application/UseCases/RunBatchUpdateUseCase.cs`, FR-017/FR-018/FR-019) (depends on T083, T084, T023)
- [ ] T089 [US4] Implement `RevitDialogSuppressionPort.GetWorkshareStatus` with `WorksharingUtils.GetCheckoutStatus`/`GetWorksharingTooltipInfo` (`src/BatchParamUpdate.Adapters.Revit/DialogSuppression/RevitDialogSuppressionPort.cs`, research.md §b layer 1, FR-024) (depends on T085)
- [ ] T090 [US4] Implement `RevitDialogSuppressionPort.SuppressNativeDialogsDuringBatch` with `IFailuresPreprocessor` + `UIApplication.DialogBoxShowing` (`src/BatchParamUpdate.Adapters.Revit/DialogSuppression/RevitDialogSuppressionPort.cs`, research.md §b layer 2, FR-023) (depends on T089)
- [ ] T091 [US4] Implement the proactive Model Group gate (`element.GroupId != ElementId.InvalidElementId`) before each Instance-level write (`src/BatchParamUpdate.Adapters.Revit/Writing/RevitParameterWritePort.cs`, research.md §c, FR-025) (depends on T084)
- [ ] T092 [US4] Implement `RevitParameterWritePort.ExecuteInstanceUpdate` inside a single `Transaction`, applying worksharing and Model Group gates per element (`src/BatchParamUpdate.Adapters.Revit/Writing/RevitParameterWritePort.cs`, FR-017/FR-019/FR-020/FR-021) (depends on T090, T091)
- [ ] T093 [US4] Implement `RevitParameterWritePort.ExecuteTypeUpdate` writing the shared `ElementType`/`FamilySymbol` at model level (`src/BatchParamUpdate.Adapters.Revit/Writing/RevitParameterWritePort.cs`, FR-018) (depends on T092)
- [ ] T094 [P] [US4] Create `ReplacementValueViewModel` with empty replacement-value validation (`src/BatchParamUpdate.UI.Wpf/ViewModels/ReplacementValueViewModel.cs`, FR-015/FR-016) (depends on T083)
- [ ] T095 [P] [US4] Create `BatchExecutionViewModel` with an inline (not popup) progress indicator during execution (`src/BatchParamUpdate.UI.Wpf/ViewModels/BatchExecutionViewModel.cs`, FR-022, SC-012) (depends on T088)
- [ ] T096 [P] [US4] Create `BatchSummaryViewModel` that shows the Instance summary (updated/skipped with reason) or Type summary (affected types + total) (`src/BatchParamUpdate.UI.Wpf/ViewModels/BatchSummaryViewModel.cs`, FR-026, SC-003/SC-004) (depends on T088)
- [ ] T097 [US4] Create `BatchParameterUpdateCommand` (`IExternalCommand`) that wires selection → discovery → replacement → execution, validating active document (`ERR-500-NO-ACTIVE-DOCUMENT`) and modifiable document (`ERR-500-DOCUMENT-NOT-MODIFIABLE`) (`src/BatchParamUpdate.Adapters.Revit/ExternalCommand/BatchParameterUpdateCommand.cs`); the `.addin` Application class is already `App` from T018 — do not switch it to command-only (depends on T017, T018, T044, T051, T067, T088)

**Checkpoint**: US1–US4 complete — the batch-update flow works end-to-end on both paths, launched from the ribbon button.

---

## Phase 7: User Story 5 - Recover what happened in a past session (Priority: P2)

**Goal**: Each session persists a readable `.txt` log and an NDJSON metrics record, both named `revit-{runId}-{documentName}`, with timings, path used, parameter/value, and outcome counts by type and category (FR-034–FR-043).

**Independent Test**: Run the add-in on either path, then locate that session's log and metrics on disk (named by `runId`+document) and confirm they describe searches, path, parameter/value, timing, and outcome.

### Tests for User Story 5

- [ ] T098 [P] [US5] Unit test: `RecordSessionUseCase` writes a `MetricsRecord.SessionStart` at session start (`tests/BatchParamUpdate.Tests.Unit/Application/RecordSessionUseCaseTests.cs`, FR-034)
- [ ] T099 [US5] Unit test: `RecordSessionUseCase` aggregates `BatchResult` with `SkippedCounts` and `CountsByCategory` grouped by classification type and category (`tests/BatchParamUpdate.Tests.Unit/Application/RecordSessionUseCaseTests.cs`, FR-042)
- [ ] T100 [US5] Unit test: `RecordSessionUseCase` emits `SessionEnd` with the correct `FinalState` on `Completed`/`Blocked`/`Cancelled` (`tests/BatchParamUpdate.Tests.Unit/Application/RecordSessionUseCaseTests.cs`, FR-034)
- [ ] T101 [US5] Unit test: a simulated `ISessionRecorderPort` failure does not interrupt the batch and produces `WARN-400-SESSION-RECORD-FAILED` via `ILoggerPort` (`tests/BatchParamUpdate.Tests.Unit/Application/RecordSessionUseCaseTests.cs`, edge case "session record cannot be written")

### Implementation for User Story 5

- [ ] T102 [US5] Create `RecordSessionUseCase` in `Application` that orchestrates emitting `MetricsRecord` (`SessionStart`/`SearchPerformed`/`ParameterSelected`/`PhaseTiming`/`BatchResult`/`SessionEnd`) via `ISessionRecorderPort` (`src/BatchParamUpdate.Application/UseCases/RecordSessionUseCase.cs`, FR-039–FR-043) (depends on T029, T030, T023)
- [ ] T103 [P] [US5] Create utility `PhaseTimer` that measures elapsed time of the discovery phase and the execution phase (`src/BatchParamUpdate.Core/PhaseTimer.cs`, FR-039)
- [ ] T104 [US5] Implement `NdjsonSessionRecorder.Record` serializing each `MetricsRecord` with `System.Text.Json` and writing with `File.AppendAllText` under `%TEMP%\juanManriqueHexagon\TRACKER\revit-{runId}-{documentName}.ndjson` (`src/BatchParamUpdate.Adapters.Persistence/NdjsonSessionRecorder.cs`, research.md §f, FR-043) (depends on T029, T030, T031, T032)
- [ ] T105 [US5] Implement write-failure handling in `NdjsonSessionRecorder`: catch the exception, emit `WARN-400-SESSION-RECORD-FAILED` via `ILoggerPort`, and do not propagate the failure to the in-flight batch (`src/BatchParamUpdate.Adapters.Persistence/NdjsonSessionRecorder.cs`) (depends on T104)
- [ ] T106 [US5] Configure `SessionFileLogger` output path to `%TEMP%\juanManriqueHexagon\LOGS\revit-{runId}-{documentName}.txt` using `RunIdGenerator` and `DocumentNameSanitizer` (`src/BatchParamUpdate.Core/SessionFileLogger.cs`, FR-035–FR-038) (depends on T033, T031, T032)
- [ ] T107 [US5] Connect recording of each `ElementSkip` (code + message) during `RunBatchUpdateUseCase` to `ILoggerPort` (`src/BatchParamUpdate.Application/UseCases/RunBatchUpdateUseCase.cs`, FR-031) (depends on T088, T028)
- [ ] T108 [US5] Connect recording of the final session summary to `ILoggerPort` when completing or blocking the session (`src/BatchParamUpdate.Application/UseCases/RecordSessionUseCase.cs`) (depends on T102)
- [ ] T109 [P] [US5] Connect `RecordSessionUseCase` to invoke `PhaseTimer` and emit `PhaseTiming("Discovery")` when `DiscoverParametersUseCase` finishes (`src/BatchParamUpdate.Application/UseCases/DiscoverParametersUseCase.cs`, FR-039) (depends on T067, T103)
- [ ] T110 [US5] Connect `RecordSessionUseCase` to invoke `PhaseTimer` and emit `PhaseTiming("Execution")` when `RunBatchUpdateUseCase` finishes (`src/BatchParamUpdate.Application/UseCases/RunBatchUpdateUseCase.cs`, FR-039) (depends on T088, T103)

**Checkpoint**: All 5 user stories work independently — traceability/observability complete.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Packaging/installer (FR-044–FR-048, not tied to a specific `spec.md` story), supported-version documentation, and in-memory end-to-end tests that cross several stories.

- [ ] T111 [P] Create interface `IInstallerPort` with `DetectInstalledRevitYears`/`Install`/`Update`/`Uninstall` (`src/BatchParamUpdate.Domain/Ports/IInstallerPort.cs`, contracts/ports.md §7)
- [ ] T112 [P] Create in-memory fake of `IInstallerPort` for tests (`tests/BatchParamUpdate.Tests.Unit/Fakes/FakeInstallerPort.cs`) (depends on T111)
- [ ] T113 [P] Unit test: `InstallerPackage.SupportedRevitYears` is a closed list `{2025, 2026, 2027}` and never offers install for a year outside it (`tests/BatchParamUpdate.Tests.Unit/Domain/InstallerPackageTests.cs`, FR-046, SC-009)
- [ ] T114 [P] Create entity `InstallerPackage` (`src/BatchParamUpdate.Domain/Model/InstallerPackage.cs`: `SupportedRevitYears`, `DetectedRevitYears`, `Actions`, data-model.md §10) (depends on T111)
- [ ] T115 Implement `RevitInstallerAdapter.DetectInstalledRevitYears` reading `HKEY_LOCAL_MACHINE\SOFTWARE\Autodesk\Revit\{year}` (and its `WOW6432Node` reflection) (`src/BatchParamUpdate.Installer/RevitInstallerAdapter.cs`, research.md §h, FR-047) (depends on T111)
- [ ] T116 Implement `RevitInstallerAdapter.Install`/`Update`/`Uninstall` copying the `Adapters.Revit` assembly and its `.addin` manifest (`Application` = `App`) per year, resolving the Revit 2027-specific destination path (`src/BatchParamUpdate.Installer/RevitInstallerAdapter.cs`, research.md §h Revit 2027 risk) (depends on T115)
- [ ] T117 [P] Create `InstallerViewModel` exposing detected years and Install/Update/Uninstall actions (`src/BatchParamUpdate.Installer/ViewModels/InstallerViewModel.cs`, FR-047) (depends on T114)
- [ ] T118 [P] Create the installer WPF view (detected-version list + action buttons) (`src/BatchParamUpdate.Installer/Views/InstallerWindow.xaml`) (depends on T117)
- [ ] T119 [P] Create per-year `.addin` manifests (2025/2026/2027) with matching add-in paths, including Revit 2027's new schema (`PublicAssemblies`/`Dependencies`), each registering `App` as the Application class (`src/BatchParamUpdate.Adapters.Revit/ExternalCommand/BatchParamUpdate.2025.addin`, `.2026.addin`, `.2027.addin`, research.md §h) (depends on T018)
- [ ] T120 [P] Configure the Velopack packaging script (`vpk pack -u BatchParamUpdate -e Installer.exe`) for the `Installer` project (`src/BatchParamUpdate.Installer/pack.ps1`, research.md §h, FR-044)
- [ ] T121 [P] Write repository `README.md` stating explicitly which Revit versions the add-in supports (2025/2026/2027) and that no other version is supported (FR-045/FR-046, SC-009)
- [ ] T122 [P] In-memory end-to-end unit test (all fakes) of the full Instance path: selection → discovery → replacement → execution → summary (`tests/BatchParamUpdate.Tests.Unit/Application/EndToEndInstancePathTests.cs`)
- [ ] T123 [P] In-memory end-to-end unit test of the full Type path, including the non-blocking inline warning (`tests/BatchParamUpdate.Tests.Unit/Application/EndToEndTypePathTests.cs`)
- [ ] T124 Manually run the 6 scenarios in `quickstart.md` against a real Revit install (launching from the dedicated ribbon button) and document results, including the 2 risks marked "re-verify" in `research.md` (Type-path on Model Group; Revit 2027 `.addin` manifest schema) (depends on T097, T116, T119)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: no dependencies — can start immediately. Ribbon/`App` tasks (T015–T019) depend on T006/T008 and must exist before `IExternalCommand` (T097).
- **Foundational (Phase 2)**: depends on Setup — BLOCKS all user stories.
- **User Stories (Phase 3–7)**: all depend on Foundational. US1 (P1), US2 (P1), and US3 (P1) are mutually independent once Foundational is done. US4 (P1) depends on at least the `IElementSelectionPort`/`ReplacementOperation` interfaces from US1/US3 (not their full implementation) to compose `BatchParameterUpdateCommand` (T097); in practice the recommended order is US1 → US2 → US3 → US4 given all are P1 and T097 integrates the four. US5 (P2) depends on `RunBatchUpdateUseCase`/`DiscoverParametersUseCase` existing (T067, T088) to instrument timing and results.
- **Polish (Phase 8)**: depends on desired stories being complete; T124 depends explicitly on T097 (integrated command) and T116/T119 (installer).

### User Story Dependencies

- **US0 (P1, Setup)**: ribbon/`App`/icons — T015–T019 after T006/T008.
- **US1 (P1)**: can start after Foundational. No dependency on other stories.
- **US2 (P1)**: can start after Foundational; extends the same files US1 creates (`EstablishSelectionUseCase`, `RevitElementSelectionPort`, `SelectElementsViewModel`) — recommended sequence US1 → US2 to avoid file conflicts, though they are conceptually independent.
- **US3 (P1)**: can start after Foundational; only requires `SelectionContext` (T041, from US1) as an input type — does not require US1/US2 to be "functionally finished".
- **US4 (P1)**: domain modeling (`SkipReason`, `WorkshareStatus`, `BatchExecutionResult`, ports) can start after Foundational in parallel with US1–US3; final integration (T097) requires US1, US2, US3, and US0 complete.
- **US5 (P2)**: modeling (mostly already covered by Foundational) can start in parallel; timing instrumentation (T109/T110) requires `DiscoverParametersUseCase`/`RunBatchUpdateUseCase` from US3/US4.

### Parallel Opportunities

- All Setup `[P]` tasks (T002, T003, T009, T010, T013, T014, T015) in parallel.
- All Foundational `[P]` tasks (14 of 18 tasks) in parallel, respecting the 4 documented sequential dependencies (T023, T026, T030, T033).
- Once Foundational is complete: **US1, US2 (after US1), US3, and US4/US5 domain modeling can proceed on parallel work branches** by different developers, as long as each respects the intra-story dependencies documented above.
- Within each story, every task marked `[P]` touches a different file from any other `[P]` task in that same phase and can run in parallel.

---

## Parallel Example: User Story 3

```bash
# US3 tests in parallel (distinct test files):
Task: "Unit test: ParameterCandidate deduplication by Binding in tests/BatchParamUpdate.Tests.Unit/Domain/ParameterCandidateSetTests.cs"
Task: "Unit test: SharedSearchQuery filters both sets in tests/BatchParamUpdate.Tests.Unit/Domain/SharedSearchQueryTests.cs"

# US3 domain entities in parallel (distinct files, no deps between them):
Task: "Create ParameterCandidate in src/BatchParamUpdate.Domain/Model/ParameterCandidate.cs"
Task: "Create ResolvedType + ExecutionScope in src/BatchParamUpdate.Domain/Model/ExecutionScope.cs"

# After T059, InstanceParameterCandidateSet and TypeParameterCandidateSet in parallel:
Task: "Create InstanceParameterCandidateSet in src/BatchParamUpdate.Domain/Model/InstanceParameterCandidateSet.cs"
Task: "Create TypeParameterCandidateSet in src/BatchParamUpdate.Domain/Model/TypeParameterCandidateSet.cs"

# US3 UI in parallel after T070 (distinct view files):
Task: "Create Dialog Box 1 in src/BatchParamUpdate.UI.Wpf/Views/InstanceParameterDialog.xaml"
Task: "Create Dialog Box 2 in src/BatchParamUpdate.UI.Wpf/Views/TypeParameterDialog.xaml"
```

---

## Implementation Strategy

### MVP First

The 4 P1 stories (US1, US2, US3, US4) plus US0 (ribbon launch) are required together for a demonstrable end-to-end MVP (without US0 there is no host entry point; without US2 a user with no preselection is blocked; without US3 there is no way to choose a parameter; without US4 there is no real update). Suggested MVP scope is therefore **Setup (including T015–T019) + Foundational + US1 + US2 + US3 + US4** (T001–T097): lets a user, with or without prior selection, launch from the dedicated ribbon button, choose an Instance or Type parameter from the two simultaneous dialogs, and run the full batch update with a summary. US5 (traceability/logging, P2) is the first candidate to defer if time is tight, since the spec marks it explicitly as "the tool remains usable end-to-end without it".

### Incremental Delivery

1. Complete Setup + Foundational → foundation ready (T001–T037), including ribbon/`App`.
2. Add US1 → test independently (`EstablishSelectionUseCase` with pre-existing selection) (T038–T047).
3. Add US2 → test independently (manual pick + cancellation) (T048–T054).
4. Add US3 → test independently (two dialogs, shared search, inline warning) (T055–T073).
5. Add US4 → **full MVP**: end-to-end batch update on both paths (T074–T097).
6. Add US5 → complete traceability/observability (T098–T110).
7. Polish: Velopack installer, version documentation, end-to-end tests, manual `quickstart.md` validation (T111–T124).

### Parallel Team Strategy

With several developers, after completing Foundational together:

- Developer A: US1 → US2 (they share files, sequence between them)
- Developer B: US3 (independent, only needs `SelectionContext` from US1 already created)
- Developer C: US4 domain modeling (`SkipReason`, `WorkshareStatus`, `BatchExecutionResult`, ports — T079–T087) in parallel, integrating with A/B at T088/T097 once their dependencies are ready
- Developer D: US5 domain/infra modeling (T098–T106, mostly independent) and installer Polish (T111–T121)

---

## Notes

- `[P]` = distinct file, no dependency on an incomplete task.
- The `[US#]` label maps each task to its story for traceability; Setup/Foundational/Polish have no label (US0 ribbon work lives under Setup).
- Each task is deliberately small and single-file (or a file + its test) to allow parallel, story-specific work branches.
- Tests are written before the implementation they validate, within each story.
- Verify that tests fail before implementing.
- Stop at each checkpoint to validate the story independently.
- No FR/SC/entity number used here is new relative to `spec.md`, `data-model.md`, or `contracts/ports.md` except FR-049–FR-051 / SC-014 / US0 introduced for ribbon bootstrap.
