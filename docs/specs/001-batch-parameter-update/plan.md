# Implementation Plan: Batch Parameter Update Revit Add-in

> **Post-implementation note (2026-09-02):** This plan predates two changes. The
> Type-parameter path it describes (Dialog Box 2, `TypeScope`, `ExecuteTypeUpdate`,
> the model-wide blast radius) was **removed** — only the instance path shipped. The
> flow was also refactored onto a single `BatchUpdateCoordinator` with event-based
> tracing (`WorkflowEvent` / `SessionTraceListener`), and the installer targets the
> per-user add-ins folder. See the reconciliation note at the top of [spec.md](./spec.md).

**Branch**: `001-batch-parameter-update` | **Date**: 2026-08-31 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `docs/specs/001-batch-parameter-update/spec.md`

## Summary

Revit add-in (WPF, C#/.NET, Revit API 2025/2026) that batch-updates a
text parameter across a selection of elements, resolved automatically by
the parameter's real Revit binding: Instance-bound (Dialog Box 1) or
Type-bound (Dialog Box 2), both shown at once and filtered by a single
shared search. The technical approach is a Domain/Hexagonal architecture
with ports & adapters: `Domain`/`Application` hold all parameter-discovery
logic, 400/500 error classification, and batch orchestration without
referencing the Revit API, while `Adapters.Revit` implements that logic
against the real API (shared source compiled by a thin project per
Revit year) and
`Adapters.Persistence` implements `.txt` log and NDJSON metrics
persistence under `%TEMP%\juanManriqueHexagon`.

Host bootstrap: Revit loads `App` (`IExternalApplication`) → `OnStartup`
creates a custom ribbon panel and a dedicated push-button (artwork from
the provided lineal-color optimization icons) → the button launches the
batch `IExternalCommand` → that command composes Application use cases.
A Velopack installer with an interactive UI (following researched
patterns / reference sources) detects installed Revit versions and
installs/updates/uninstalls the add-in per year.

## Technical Context

**Language/Version**: C# 12 / .NET — `net8.0-windows` for the Revit 2025
and 2026 year projects (see
`research.md` §a; documented risk on the real patch level of 2025/2026
on the evaluation machine).

**Primary Dependencies**: Autodesk Revit API (`RevitAPI.dll`,
`RevitAPIUI.dll`, referenced locally per year, never redistributed);
WPF (`Microsoft.WindowsDesktop.App.WPF`, included in the Windows Desktop
SDK); Velopack (packaging/update of the `Installer`); xUnit (tests for
`Domain`/`Application`). No NLog/Serilog and no mocking frameworks —
see `research.md` §e and §g for keeping the dependency surface minimal.

**Storage**: No database. File-based persistence on the local filesystem:
`.txt` (session log) and `.ndjson` (session metrics) under
`%TEMP%\juanManriqueHexagon\{LOGS,TRACKER}`.

**Testing**: xUnit for `Domain`/`Application` with hand-written port fakes
(no Revit, no mocking framework — `research.md` §g). Manual end-to-end
validation of the real `Adapters.Revit` against a Revit session following
`quickstart.md` (not automated; requires installed Revit and a license,
out of CI scope for this exercise).

**Target Platform**: Windows 10/11 desktop, inside the Autodesk Revit
2025 or 2026 process (add-in), plus a standalone WPF process for
the Velopack `Installer` (standalone installer host, following researched
patterns / reference sources).

**Project Type**: Desktop add-in (host: Autodesk Revit) + independent
desktop installer. Not a web application or a service; the template's
"web application" and "mobile+API" layouts do not apply.

**Performance Goals**: Not quantified by the spec (no throughput SC).
Qualitative constraint from FR-022/SC-012: the UI must stay responsive
during the batch (inline, non-blocking progress), which implies running
the batch off the WPF UI thread and marshaling progress updates back to
the UI thread, while actual model writes stay on the Revit API thread
(a Revit API constraint, not an add-in design choice).

**Constraints**: No network connection or external service (FR-037, Out of
Scope); a single reversible `Transaction` per batch (FR-019); fixed,
non-configurable persistence locations (Assumptions); no native Revit
dialog may require per-element manual approval during the batch (FR-023);
launch is via a dedicated ribbon panel/button registered by `App`
(FR-049–FR-051).

**Scale/Scope**: One active Revit document per session (Out of Scope:
multi-document and linked-model elements); typical interactive-session
selection size (tens to a few thousand elements), not an "entire model"
use case except as the already-documented Type-path side effect.

## Constitution Check

*GATE: Must be evaluated before Phase 0 and re-evaluated after Phase 1.*

> This repository had no formal `.specify/memory/constitution.md` before
> this plan. A minimal version (`.specify/memory/constitution.md`, v1.0.0)
> was created, derived literally from the "Assumptions & Mandated
> Constraints" section of `spec.md`: the four stakeholder-mandated pillars
> (Functionality, Traceability, Observability, Testability) plus the
> Domain/Hexagonal ports-and-adapters architectural mandate. This section's
> gate is evaluated against those five principles.

| Principle | Evaluation | Status |
|---|---|---|
| I. Functionality | The design executes both paths (Instance/Type) inside a single reversible `Transaction` (FR-019); no element is left partially modified if the global operation cannot proceed (research.md §b, `Batch Execution Result` in data-model.md). Launch is a dedicated ribbon button from `App.OnStartup` (FR-049–FR-051). | PASS |
| II. Traceability | Each session generates its own `runId` (research.md §f), and the `.txt`/`.ndjson` pair named `revit-{runId}-{documentName}` reconstructs searches, applied parameter/value, path used, and outcome without Revit open (FR-034–FR-043). | PASS |
| III. Observability | 400/500 catalog formalized in `data-model.md` (`Error/Warning Code Catalog`), with non-technical messages and aggregated counts by type/category in the `Session Metrics Record` (FR-027–FR-033, FR-042). | PASS |
| IV. Testability | `Domain`/`Application` do not reference `RevitAPI.dll`; they are exercised with port fakes in xUnit (research.md §g). Ribbon registration is adapter-only and is not a domain port. | PASS |
| V. Hexagonal Architecture (ports & adapters) | The seven ports in `contracts/ports.md` are the only contact surface between `Domain`/`Application` and the outside world (Revit, filesystem, installer). Only the `Adapters.Revit.20XX` year projects reference `RevitAPI.dll`. Ribbon/`App` live in the shared adapter source and do not add an eighth port. | PASS |

**Result**: No violations. Complexity Tracking below documents design
decisions that depart from the "simplest" option for reasons already
mandated by the spec, not constitution-gate violations.

*(Re-evaluated after Phase 1 — see the end of this document: no changes;
the `data-model.md`/`contracts/` design introduces no `Domain`/`Application`
dependency on Revit or on concrete infrastructure.)*

## Project Structure

### Documentation (this feature)

```text
docs/specs/001-batch-parameter-update/
├── spec.md                          # Existing (approved)
├── checklists/
│   └── requirements.md              # Existing (iteration history)
├── plan.md                          # This file (/speckit-plan)
├── research.md                      # Phase 0 (/speckit-plan)
├── data-model.md                    # Phase 1 (/speckit-plan)
├── quickstart.md                    # Phase 1 (/speckit-plan)
├── contracts/
│   └── ports.md                     # Phase 1 (/speckit-plan) — port contracts
└── tasks.md                         # Phase 2 (/speckit-tasks)
```

### Source Code (repository root)

```text
src/
├── BatchParamUpdate.Core/               # Cross-cutting: SessionFileLogger (canonical
│                                         #   ILoggerPort impl.), runId generation, name
│                                         #   sanitizing, shared utilities with no Revit dependency.
│
├── BatchParamUpdate.Domain/              # Pure entities (data-model.md), the 7 ports
│   ├── Model/                            #   (contracts/ports.md), Error/Warning Code Catalog,
│   ├── Ports/                            #   invariant rules. ZERO reference to RevitAPI.dll
│   └── ErrorCatalog/                     #   or to Core (types only, no concrete I/O).
│
├── BatchParamUpdate.Application/         # Use cases: DiscoverParametersUseCase,
│                                         #   RunBatchUpdateUseCase, RecordSessionUseCase.
│                                         #   Orchestrates Domain + ports; no RevitAPI.dll.
│
├── BatchParamUpdate.Adapters.Revit/      # Shared Revit-adapter source (.shproj/.projitems).
│   ├── App.cs                            #   IExternalApplication: OnStartup registers
│   ├── Resources/                        #   lineal-color optimization PNGs (64/100 sourced;
│   │                                     #   16/32 derived at implementation if needed)
│   ├── Selection/                        #   Imported by the two year projects below.
│   ├── Discovery/                        #   Only this source tree talks to RevitAPI.dll
│   ├── Writing/                          #   (via the year shells).
│   ├── DialogSuppression/
│   ├── ExternalCommand/                  #   IExternalCommand (Application class = App)
│   └── Year.props                        #   Shared TFM / HintPath / Debug deploy
├── BatchParamUpdate.Adapters.Revit.2025/ # Thin year shell: net8.0-windows + Revit 2025 API + .addin
├── BatchParamUpdate.Adapters.Revit.2026/ # Thin year shell: net8.0-windows + Revit 2026 API + .addin
│
├── BatchParamUpdate.Adapters.Persistence/ # IMetricsPort/ISessionRecorderPort:
│                                          #   NDJSON + .txt under %TEMP%\juanManriqueHexagon
│                                          #   (uses Core.SessionFileLogger internally).
│
├── BatchParamUpdate.UI.Wpf/               # Single window with Dialog Box 1 + Dialog Box 2 +
│                                          #   shared search + replacement step + inline progress
│                                          #   + summary (ViewModels consume Application).
│
└── BatchParamUpdate.Installer/            # WPF host packaged with Velopack (research.md §h):
                                           #   detect installed Revit + install/update/uninstall
                                           #   per year.

tests/
└── BatchParamUpdate.Tests.Unit/           # xUnit. References Domain + Application only.
    ├── Domain/                            #   Hand-written fakes of the 7 ports (research.md §g).
    └── Application/
```

**Architecture flow** (host → use cases):

```text
Revit
  → App.cs (IExternalApplication.OnStartup)
      → custom RibbonPanel + dedicated PushButton (16/32 images from Resources/)
          → IExternalCommand (BatchParameterUpdateCommand)
              → Application use cases (selection, discovery, batch, session)
```

**Structure Decision**: Single solution (`BatchParamUpdate.sln`) with
projects split by hexagonal layer. Revit years are two thin projects
that import one Shared Project (`Adapters.Revit`), not six
solution configurations and not three separate solutions. `Domain` and
`Application` are year-neutral Class Libraries (`net8.0`, no `-windows`,
no WPF or Revit dependency) so `Tests.Unit` can exercise them with no UI
or host loaded. Only the `Adapters.Revit.20XX` shells reference
`RevitAPI.dll`/`RevitAPIUI.dll` (research.md §a). `Core`,
`Adapters.Persistence`, `UI.Wpf`, and `Installer` compile once
(`net8.0-windows`) and are referenced the same way from any Revit year
because they never touch `RevitAPI.dll` directly. That keeps one shared
codebase (FR-048) while still emitting a distinct add-in per year.

The `.addin` manifest points at the `Application` class (`App`), not
command-only registration. Ribbon wiring is adapter-only (YAGNI: no
`IRibbonHost` domain port).

## Complexity Tracking

> The two most notable deviations from the simplest possible option,
> both already mandated by `spec.md` and not introduced by this plan.

| Deviation | Why it is needed | Simpler alternative rejected because |
|---|---|---|
| Per-year adapter projects (`Adapters.Revit.2025`/`.2026`) sharing one `.projitems` source tree | FR-048 mandates 2025/2026 from one codebase; a single csproj with year-named configurations cannot emit both years in one solution build | Six `Debug20XX` configurations produce only one year per build |
| 7 ports/interfaces in `Domain` instead of calling the Revit API directly from business logic | Explicit architectural mandate (Assumptions → "Architecture") so discovery/classification/orchestration logic is testable without Revit running (Testability pillar) | Without ports, `Domain`/`Application` would depend on `RevitAPI.dll`, unit tests would require Revit installed and running, contradicting the mandated Testability pillar |

## Progress Tracking

*This checklist is updated during `/speckit-plan` execution.*

**Phase Status**:
- [x] Phase 0: Research complete (`research.md`)
- [x] Phase 1: Design complete (`data-model.md`, `contracts/ports.md`, `quickstart.md`)
- [x] Phase 2: Task planning described (`tasks.md`)
- [ ] Phase 3: Tasks generated (`/speckit-tasks`) — `tasks.md` exists; implementation not started
- [ ] Phase 4: Implementation complete
- [ ] Phase 5: Validation pass (`quickstart.md` executed against real Revit)

**Gate Status**:
- [x] Initial Constitution Check: PASS
- [x] Post-Design Constitution Check: PASS (unchanged vs. the initial gate)
- [x] All `NEEDS CLARIFICATION` items for this feature resolved (see `research.md`; remaining open points are explicit validation risks, not blocking clarifications)
- [x] No unjustified deviations in Complexity Tracking

**Phase 2 — what `/speckit-tasks` produced**: `tasks.md` breaks down, per
`Project Structure` project, concrete implementation tasks ordered by
dependency (Domain → Application → Adapters → UI → Installer), grouped by
user story (US0 ribbon launch, then US1–US5 of `spec.md`) so incremental
verifiable deliveries are possible with `Tests.Unit` and the
`quickstart.md` scenarios. Ribbon/`App` tasks sit in Setup so
`IExternalApplication` exists before `IExternalCommand`.
