# Phase 0 Research: Batch Parameter Update Revit Add-in

**Feature**: `001-batch-parameter-update` | **Date**: 2026-08-31
**Spec**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md)

This document resolves, with a reasoned decision, every technical point
that the "Technical Context" section of `plan.md` would have left as
`NEEDS CLARIFICATION` if the spec had not already fixed the stack.
`spec.md` mandates the *what* (WPF, C#/.NET, Revit API 2025-2027,
hexagonal architecture, Velopack, NDJSON, Temp locations); this document
decides the concrete *how*. Every decision cites the originating
FR/Assumption from `spec.md` and is closed (none left open).

---

## a) Multi-target Revit 2025-2027 from a single solution

**Decision**: One Visual Studio Shared Project holds all Revit-adapter
source (`src/BatchParamUpdate.Adapters.Revit/*.cs`, `.projitems`). Three
thin SDK-style year projects import that source and bind to that year's
API — the same scheme as ipx.bimops (`ipx.bimops.revit.2025` / `.2026`
importing `ipx.bimops.revit.ui.projitems`) and IP Catalog
(`IPX.Catalog.Revit.2025` / `.2026`). Solution configurations stay
`Debug`/`Release`; the year is the **project**, not the configuration
name. A `Debug` solution build therefore produces 2025, 2026, and 2027
outputs side by side.

| Project | `TargetFramework` | Referenced `RevitAPI.dll` | `DefineConstants` |
|---|---|---|---|
| `Adapters.Revit.2025` | `net8.0-windows` | `%ProgramFiles%\Autodesk\Revit 2025\RevitAPI.dll` | `REVIT2025;REVIT2025_OR_GREATER` |
| `Adapters.Revit.2026` | `net8.0-windows` | `%ProgramFiles%\Autodesk\Revit 2026\RevitAPI.dll` | `REVIT2026;REVIT2025_OR_GREATER;REVIT2026_OR_GREATER` |
| `Adapters.Revit.2027` | `net10.0-windows` | `%ProgramFiles%\Autodesk\Revit 2027\RevitAPI.dll` | `REVIT2027;REVIT2025_OR_GREATER;REVIT2026_OR_GREATER;REVIT2027_OR_GREATER` |

Each year project sets `RevitYear`, imports
`src/BatchParamUpdate.Adapters.Revit/Year.props` (HintPath, `Private=false`,
`CopyLocal=false`, TFM, constants, Debug copy into
`%AppData%\Autodesk\REVIT\Addins\{year}`), and ships its own `.addin`
with `Assembly` = `BatchParamUpdate\BatchParamUpdate.Adapters.Revit.{year}.dll`.
Version-conditioned code uses `REVITXXXX_OR_GREATER` symbols so adding
Revit 2028 is a new thin project plus an additive `#if`, not a
configuration-matrix change.

`Domain` / `Application` / `Core` / `UI.Wpf` / `Installer` stay
year-neutral and are not duplicated.

**Rationale**: FR-048 requires one shared codebase/solution. A year
project plus shared `.projitems` keeps one code tree, lets a single
solution build emit every supported year, gives each year its own
assembly name and `.addin`, and matches the production pattern already
used in ipx.bimops. Debug deploy per year is then a PostBuild on that
project, not a configuration switch.

**Alternatives considered**:
- *One `.csproj` with `Debug2025`/`Release2027` configurations*
  ([`RevitAPI_MultiVersion_Setup`](https://github.com/HariharanRadha09585/RevitAPI_MultiVersion_Setup)):
  rejected. A solution build then produces only one year; CI needs a
  configuration matrix; assembly identity and `.addin` stay shared
  unless further conditioned. That is the opposite of the bimops local
  loop (build once, three add-ins on disk).
- *A single TFM (`net8.0-windows`) for all three versions*: **rejected**
  after investigating Revit's real state as of this plan date
  (2026-08-31): Autodesk is migrating **Revit 2025 and 2026 from .NET 8 to
  .NET 10** via the 2025.5/2026.5 updates (GA "first week of August 2026",
  already past as of this document — see Risks below), and **Revit 2027
  ships directly on .NET 10** (`net10.0-windows`) with no .NET 8 build.
  Fixing a single TFM for 2025-2027 would have been wrong for 2027 and
  potentially wrong for 2025/2026 depending on the patch level on the
  evaluator machine.
- *NuGet `Revit_All_Main_Versions_API_x64`*: evaluated as a way to avoid
  managing `HintPath`s by hand; rejected for now because it introduces a
  third-party dependency for something the evaluation environment (with
  Revit already installed) resolves deterministically with local paths,
  and because a Stack Overflow thread reviewed during this research
  documents real friction between that package and IL-weaving tools on
  `net8.0`; not ruled out as a future improvement.

**Open risk for the stakeholder** *(see also plan.md → Complexity
Tracking)*: if the evaluation machine already has the 2025.5/2026.5
(.NET 10) update for Revit 2025/2026, the 2025/2026 **projects**
in the table above must point at `net10.0-windows` instead of
`net8.0-windows`, with no API changes. Before building the final
installer, verify the Revit 2025/2026 patch level on the
build/evaluation machine and adjust those two projects'
`TargetFramework` if needed. This does not change any architecture
decision, only the TFM of two of the three year projects. Building
`Adapters.Revit.2027` requires a .NET 10 SDK; without it, build that
project only after the SDK is installed — 2025/2026 still compile. The
`.sln` lists the 2027 project but does not include it in the default
solution build so `dotnet build` stays green without a .NET 10 SDK.

---

## b) Suppressing native Revit dialogs during the batch

**Decision**: Two-layer mechanism inside a single `Transaction`:

1. **Primary (proactive, deterministic)**: before attempting to write an
   element, check its worksharing state with
   `WorksharingUtils.GetCheckoutStatus(document, element.Id)` (or
   `GetWorksharingTooltipInfo` for the owner name) **without** executing
   any UI action. If the element is *CheckedOutToOtherUser* (or the model
   is workshared and the element is not editable by the current user),
   mark it for *skip* with the corresponding 400 code and **do not** call
   `Parameter.Set(...)`. This API check never fires a native dialog by
   itself: the "Editing Requires Ownership" dialog appears when the
   action originates from Revit's interactive UI, not when the add-in
   calls the API directly — so the proactive check is the main defense,
   not reactive popup suppression.
2. **Defensive (safety net)**: in case some other action (`SetParam`,
   resolving a `FailureDefinitionId` with `Warning`/`Error` severity, or
   a dialog raised by an internal Revit event during the transaction)
   tries to show UI:
   - An `IFailuresPreprocessor` assigned via
     `Transaction.SetFailureHandlingOptions(...)` that, in
     `PreprocessFailures(FailuresAccessor)`, walks
     `GetFailureMessages()` and **deletes** (`DeleteWarning`) every
     `FailureMessageAccessor` of severity `Warning`, and for severity
     `Error` records a failure for the current element and
     `ResolveFailures`/aborts only that sub-step (see the
     transactionality note in `data-model.md` → Batch Execution Result),
     so `ShowFailuresDialog` is never invoked.
   - A `UIApplication.DialogBoxShowing` handler subscribed only for the
     batch execution window, which calls
     `args.OverrideResult((int)TaskDialogResult.Cancel)` (or the
     corresponding no-op result) for any native dialog that still tries
     to show, and logs its appearance — never let a dialog block the
     Revit thread during a batch.

**Rationale**: FR-023/FR-024 require that no native dialog needs
per-element manual approval during the batch. Using only
`DialogBoxShowing` without the proactive check would be reactive and
less traceable (you would not know *why* an element was skipped until
inspecting the suppressed dialog); using only the proactive check
without the `IFailuresPreprocessor`/`DialogBoxShowing` safety net would
leave unanticipated cases exposed (e.g. a Revit warning unrelated to
worksharing). The two layers together satisfy SC-011 without exposing
the user to any popup.

**Alternatives considered**:
- *`IFailuresPreprocessor` only*: does not cover dialogs that do not
  originate as a `FailureMessage` (some native Revit `TaskDialog`s do
  not go through the Failures API).
- *`DialogBoxShowing` only*: does not allow the skip decision **before**
  touching the element, which makes producing the per-element summary
  with the exact skip reason required by FR-021/SC-004 harder.

---

## c) Detecting and proactively skipping Model Group member elements

**Decision**: Before attempting any Instance-level write on an element,
check `element.GroupId != ElementId.InvalidElementId`. This property and
comparison are stable on Revit 2025, 2026, and 2027 (`ElementId` changed
from `int` to `long` in Revit 2024 and its relevant public surface here
did not change again: `ElementId.InvalidElementId` remains the correct
sentinel on all three target versions, so no year-conditioned code is
needed for this check). The check runs at two moments, both before
`Parameter.Set(...)`:

1. When building the Instance Parameter Candidate Set (Dialog Box 1), so
   discovery does not hide the parameter (it remains a union, not an
   intersection — FR-007), but the grouped element is marked internally
   as "not eligible for write" for step 2.
2. Immediately before writing each element during batch execution, as
   the last gate — never enter "Edit Group" mode, never automatic
   ungroup/regroup (Out of Scope in spec.md).

**Rationale**: FR-025/FR-033/SC-013 require exactly this behavior:
proactive skip, not reactive (not "try and catch the exception"), and
no group restructuring. spec.md already documents (Assumptions,
checklist iteration 5) the prior research behind this Revit API
restriction; this research.md only fixes the exact detection mechanism.

**Alternatives considered**:
- *Try `Parameter.Set` and catch the exception/warning*: rejected
  explicitly by spec.md (that is the unstable pattern the Revit
  community discourages for single-instance groups, and it hard-fails
  for groups with more than one instance).
- *Ungroup → edit → regroup*: excluded explicitly in "Out of Scope".

**Note for Type-path**: because a Type-level update writes the shared
`ElementType`/`FamilySymbol` object (not the grouped instance), this
restriction is **expected not** to apply — as spec.md already states.
This is re-verified when running `quickstart.md` (Type-path scenario
with a Model Group element in scope), not assumed in code without a
manual check.

---

## d) Instance vs Type parameter discovery (deduplicated union)

**Decision**:

- **Instance Parameter Candidate Set (Dialog Box 1)**: for each element
  `e` in the selection scope, iterate `e.Parameters` (instance-bound
  parameters). Include a parameter `p` if
  `p.StorageType == StorageType.String` (this is what spec.md calls
  "text/string storage" — the check that survives unchanged on
  2025/2026/2027, unlike `Definition.ParameterType`, which Revit has
  been deprecating since 2022 in favor of `Definition.GetDataType()`
  returning a `ForgeTypeId`; `ForgeTypeId` is not used as the primary
  filter because the spec asks for text *storage*, not a UI semantic
  category) and `!p.IsReadOnly`. The deduplication key is
  `p.Definition.Name` (see the collision note below).
- **Type Parameter Candidate Set (Dialog Box 2)**: for each element `e`
  in scope, resolve its type with
  `document.GetElement(e.GetTypeId())` and apply the same filter
  (`StorageType.String`, not read-only) on `elementType.Parameters`.
- Both collections are built independently (not one from the other): the
  same parameter name can appear in both lists if, for different
  families in scope, that name is Instance-bound on one family and
  Type-bound on another — consistent with "union, not intersection" and
  needing no special handling: each appearance is routed to its dialog
  according to that family's real binding.
- The shared search (`Shared Search Query`) filters both in-memory lists
  (contains, case-insensitive, on `Definition.Name`) without touching
  the Revit document on every keystroke.

**Rationale**: Implements FR-007–FR-013 literally. `StorageType` is
preferred over `ForgeTypeId`/`ParameterType` because it is the most
stable property across 2025-2027 and maps 1:1 to the spec language
("text/string-typed", "text storage").

**Collision note (documented, not blocking)**: if a model defined two
distinct shared parameters with the same `Definition.Name` and the same
binding, they would be treated as one in the list (reasonable given both
would show the same text to the user and the spec does not require
disambiguation by shared-parameter GUID); marked as an assumption to
validate with the stakeholder if the real test model has that case (see
"Risks" in `plan.md`).

**Alternatives considered**:
- *Filter by `BuiltInParameterGroup` or category*: rejected; the spec is
  explicit that binding (Instance vs Type) is the only routing
  criterion, not element category.
- *Use `Definition.GetDataType() == SpecTypeId.String.Text`*: considered
  as an extra filter to exclude "URL" or "multiline text" parameters
  that are also `StorageType.String` but semantically different;
  rejected for v1 because the spec requires "text/string" without
  subtype distinctions, and excluding them would shrink the candidate
  universe without the spec asking for it — may be added as a
  post-implementation refinement if the evaluator requests it.

---

## e) Centralized logging in Core, consumed by domain/application

**Decision**: Define port `ILoggerPort` (see `contracts/ports.md`) in the
`Domain` layer. The `Core` layer provides the single canonical
implementation (`SessionFileLogger`), a **minimal in-house** plain-text
logger (no NLog/Serilog), backed by an in-memory queue
(`BlockingCollection<string>`) drained by a single background thread to
the session `.txt`, so the UI/Revit thread is not blocked on every log
line. `Domain` and `Application` do not instantiate `SessionFileLogger`
directly: they receive `ILoggerPort` by injection from the composition
root (`Adapters.Revit`/`UI.Wpf`), which does reference `Core`. This
interprets the spec.md mandate ("logging lives in Core, inherited by
domain/application") as *"domain/application consume Core's canonical
logger exclusively through the port"*, not as literal class inheritance —
implementation inheritance would violate the Dependency Inversion that
hexagonal architecture itself requires (Domain must not compile against
concrete I/O types).

**Rationale / why not NLog/Serilog despite researched reference
sources**: those sources use NLog with two targets (full + errors)
because they support multiple processes and broader retention needs.
This feature only needs **one** plain `.txt` per session, with a fixed
name (`revit-{runId}-{documentName}.txt`, FR-038), no rotation and no
multiple file-severity levels. Adding NLog/Serilog here would be another
external dependency to multi-target (`net8.0-windows` and
`net10.0-windows`) and to package with Velopack, in exchange for
features (routing, rotation, sinks) this scope does not ask for — it
does not fit "do not add a dependency if it can be avoided" given the
real size of the problem.

**Alternatives considered**:
- *NLog 5.x (pattern from researched reference sources)*: rejected for
  the reasons above; documented here as a fallback if implementation
  surfaces richer logging requirements (rotation, per-file levels) not
  covered by the current spec.
- *`Microsoft.Extensions.Logging` with a single file provider*:
  considered; rejected as heavier than needed for a single plain-text
  sink and because it adds a transitive `Microsoft.Extensions.*`
  dependency tree that would need validation against `net10.0-windows`
  before Revit 2027 is widely proven in the community.

---

## f) NDJSON metrics and .txt log format and write path

**Decision**:

- **Paths** (fixed, spec.md → Assumptions):
  `%TEMP%\juanManriqueHexagon\TRACKER\revit-{runId}-{documentName}.ndjson`
  and `%TEMP%\juanManriqueHexagon\LOGS\revit-{runId}-{documentName}.txt`.
- **`runId`**: a short GUID (`Guid.NewGuid().ToString("N").Substring(0,8)`)
  generated once at session start (command launch), not reused across
  invocations — Revit exposes no stable native "run" identifier unique
  per command invocation, so one is generated, consistent with FR-034
  ("each invocation... with its own unique identifier").
- **`documentName`**: sanitized `Document.Title` (`\ / : * ? " < > |`
  and spaces replaced by `_`, truncated to 60 characters) to guarantee a
  valid NTFS file name.
- **NDJSON**: one JSON object per line, written with `File.AppendAllText`
  (never rewriting the whole file), so a session that ends abruptly
  leaves already-written lines intact and line-parseable (the reason
  NDJSON exists, FR-043). Minimum record types (`"type"` field on each
  line): `session_start`, `search_query`, `parameter_selected`,
  `batch_result`, `session_end` — formalized as `Session Metrics Record`
  in `data-model.md`.
- **`.txt`**: ISO-8601 timestamp, level (`INFO`/`WARN`/`ERROR`), and
  message, one entry per relevant session event (scope established,
  search performed, parameter chosen, each skip with its 400/500 code,
  final summary).

**Rationale**: Implements FR-035–FR-043 literally; using line-by-line
`AppendAllText` (instead of serializing a full `List<T>` at the end) is
the decision that actually delivers the NDJSON benefit FR-043 wants
(line-by-line parseable, resilient to an abnormal Revit shutdown
mid-session).

**Alternatives considered**:
- *A single JSON array per session*: rejected explicitly by FR-043
  ("NDJSON... so that it can be queried line-by-line").
- *Local SQLite for metrics*: rejected as a stateful, schema-bearing
  mechanism the spec does not ask for, and which also contradicts the
  "no external network/service dependencies" mandate in spirit of
  simplicity (SQLite is not networked, but it is a dependency and a
  binary format that undermines exactly what NDJSON solves: inspection
  with simple line tools).

---

## g) Testing strategy for domain/application without Revit

**Decision**: `Tests.Unit` in **xUnit**, referencing only `Domain` and
`Application` (never `Adapters.Revit`, never `RevitAPI.dll`). Ports
(`IElementSelectionPort`, `IParameterDiscoveryPort`, etc. — see
`contracts/ports.md`) are implemented in the test project as
**hand-written fakes** (simple classes returning preconfigured in-memory
data), not with a mocking framework. This is intentional (YAGNI): each
port surface is small (3-6 methods), and a concrete fake class is as
readable as a `Moq`/`NSubstitute` setup, without adding that dependency
to the solution.

**Rationale**: Directly materializes the spec's "Testability" pillar:
400/500 classification logic, batch orchestration (Instance vs Type),
and skip rules (Model Group, worksharing, missing/read-only/non-text
parameter) are exercised with synthetic data without Revit installed or
running, which no test against `RevitAPI.dll` (requiring real Revit or a
heavy API stub) could give in the time of this exercise.

**Alternatives considered**:
- *Moq/NSubstitute*: not permanently rejected, only deferred: if during
  `/speckit-tasks`/implementation the number of scenarios per port grows
  enough that hand-written fakes become repetitive, migrating to `Moq`
  is a local change to `Tests.Unit` without touching
  `Domain`/`Application`.
- *Revit Test Framework / in-Revit tests (RTF, Revit Batch Processor)*:
  rejected for this feature's tests because it reintroduces a running
  Revit dependency, exactly what the Testability pillar seeks to avoid
  for business logic; reserved as a manual (not automated) complement
  documented in `quickstart.md` for the real adapters.

---

## h) Velopack: installer with version detection and interactive UI

**Decision**: A WPF `Installer` project (`net8.0-windows`, a standalone
installer host following researched patterns / reference sources) is the
app Velopack packages (`vpk pack`), **not** the Revit add-in itself
(Revit loads DLLs via an `.addin` manifest; it does not run a `.exe`).
The flow:

1. The `Installer` host references Velopack 1.2.0 and calls
   `VelopackApp.Build().Run()` as the first statement in `Program.Main`
   (before any WPF window). `vpk pack` 1.2.0 refuses binaries that omit
   this hook. `pack.ps1` is the pack entry point: it publishes the
   installer, builds year payloads 2025/2026/2027, and runs
   `vpk pack -u BatchParamUpdate -v <version> -p <Installer-publishDir> -e Installer.exe`.
   Native `dotnet` / `vpk` failures abort the script. Packing 2027
   requires a .NET 10 SDK (`net10.0-windows`); without it the script
   fails with a pointer to https://aka.ms/dotnet/download rather than
   emitting an incomplete package. The pack produces `Setup.exe` + the
   release package, signable with a certificate-signing pattern according
   to research (optional `tools/Sign` if a cert is later provided; none
   is assumed here).
2. When running `Setup.exe` / opening the installed `Installer.exe`, the
   interactive UI (FR-047):
   - Detects installed Revit versions by checking
     `HKEY_LOCAL_MACHINE\SOFTWARE\Autodesk\Revit\{year}` (and its
     `WOW6432Node` reflection on mixed 32/64-bit systems) for each of
     2025/2026/2027.
   - For each detected year, offers Install/Update/Uninstall, copying the
     `Adapters.Revit.{year}` assembly plus
     its `.addin` manifest (Application class = `App`) to the matching
     add-ins folder.
   - **Risk note (Revit 2027)**: Revit 2027 changes the "all users"
     add-ins location from `%ProgramData%\Autodesk\Revit\Addins\{year}`
     to a path under `%ProgramFiles%`, and adds new manifest keys
     (`PublicAssemblies`, `Dependencies`) for add-in isolation. The
     `Installer` must resolve the destination path per year instead of
     assuming a single fixed folder pattern, and the `.addin` generated
     for 2027 may need the extra `<manifestsettings>` block — **to
     verify against a real Revit 2027 install** during implementation,
     since that was not the primary focus of this planning research.

**Rationale**: FR-044–FR-047 and the Assumption "Installer engine"
mandate Velopack and an interactive UI following researched patterns /
reference sources. Packaging a separate WPF host (not the add-in DLL)
is the only coherent way to "launch an interactive
install/uninstall/update UI" from a Velopack artifact, because Velopack
packages and updates executable applications, not assemblies loaded by
another process.

**Alternatives considered**:
- *MSI/WiX*: rejected; Velopack is an explicit stakeholder mandate
  (Assumptions), not an option to evaluate.
- *Squirrel.Windows (legacy, still seen in parallel in researched
  reference sources)*: rejected for a new delivery; Squirrel is in
  maintenance mode and researched sources already moved their primary
  release to Velopack, leaving Squirrel only for old-client
  compatibility — not applicable here with no prior installed base.

---

## i) Ribbon registration vs command-only `.addin`, and icon sizes

**Decision**: The `.addin` manifest registers an **Application** class
(`App` implementing `IExternalApplication`), not a command-only add-in.
In `OnStartup`, `App` creates a custom `RibbonPanel` (on a dedicated
panel, not a generic Add-Ins dump) and a `PushButton` whose
`PushButtonData` targets `BatchParameterUpdateCommand` (`IExternalCommand`).
`OnShutdown` is a no-op beyond returning `Result.Succeeded`.

Graphic resources: copy the assignment assets from
`C:\Users\Juan -- IP\Descargas\icons8-optimization-lineal-color` into
`src/BatchParamUpdate.Adapters.Revit/Resources/`. The provided files are
`icons8-optimization-64.png` and `icons8-optimization-100.png`
(lineal-color optimization artwork; colors taken from that asset). Revit
ribbon buttons typically want **16px** (`Image`) and **32px**
(`LargeImage`) for Revit 2025–2027; those sizes are **derived at
implementation** from the provided 64/100 PNGs (no extra commercial
license claim). Wire them on the `PushButton` via `BitmapImage` /
`PngBitmapDecoder` from the copied resources (embedded or `Content`
with copy-to-output).

**Rationale**: FR-049–FR-051 and SC-014 require a dedicated panel and
button as the launch path. A command-only `.addin` would dump the
command onto the generic Add-Ins tab, which FR-050 explicitly forbids.
Ribbon construction is a Revit UI API concern and stays in
`Adapters.Revit`; inventing an `IRibbonHost` domain port would add an
abstraction with a single production implementation and no
domain-testable behavior (YAGNI).

**Alternatives considered**:
- *Command-only `.addin` (`AddInType` Command)*: rejected by FR-050.
- *Custom ribbon tab (not just a panel)*: not required; a custom panel
  with a dedicated button satisfies the spec. A whole extra tab can be
  added later if the evaluator asks; not built now.
- *Shipping only 64/100 without deriving 16/32*: Revit scales, but
  Autodesk guidance for 2025–2027 still recommends 16/32; derive rather
  than invent new artwork.

---

## Summary of risks/assumptions still to validate with the stakeholder

1. **Real TFM of Revit 2025/2026 on the evaluation machine** (item a):
   depends on patch level (.NET 8 vs .NET 10 after 2025.5/2026.5).
2. **Type-path behavior on Model Group elements** (item c): expected not
   to apply the restriction, but not confirmed against a real 2025-2027
   API; `quickstart.md` includes the manual scenario to close it before
   implementation sign-off.
3. **Add-in install path and manifest schema on Revit 2027** (item h):
   differs from 2025/2026; requires verification against a real Revit
   2027 install.
4. **Parameter-name collision across distinct bindings** (item d):
   documented and accepted, but not tested against a real model with
   that peculiarity.

None of these points block the design (each has a decision and a
verification plan); they are listed here and repeated in `plan.md` for
visibility before implementation.
