# Phase 1 Data Model: Batch Parameter Update Revit Add-in

**Feature**: `001-batch-parameter-update` | **Spec**: [spec.md](./spec.md) | **Research**: [research.md](./research.md)

All of the following entities live in `BatchParamUpdate.Domain` (see
`plan.md` → Project Structure) as POCOs/immutable `record`s where
reasonable, with no reference to `RevitAPI.dll`. Names match the "Key
Entities" section of `spec.md` literally. Each entity cites the FR(s)
that originate it and its validation invariants.

Ribbon panel, push-button, and graphic assets (FR-049–FR-051) are
**host/adapter concerns**, not Domain entities — see §11.

---

## 1. Selection Context

Represents the current element scope for the session and how it was
established.

| Field | Type | Notes |
|---|---|---|
| `ElementRefs` | `IReadOnlyList<ElementRef>` | `ElementRef` = Domain-owned type (not Revit's `ElementId`) wrapping an opaque string identifier + `CategoryName` (for category aggregation in `Session Metrics Record`). The `ElementId ↔ ElementRef` mapping lives in `Adapters.Revit`. |
| `Origin` | `SelectionOrigin` (enum: `PreExisting`, `ManualPick`) | FR-001–FR-005. |
| `IsValid` | `bool` (derived: `ElementRefs.Count > 0`) | FR-006. |

**Invariants**:
- `IsValid == false` blocks the transition to discovery (FR-006) →
  produces catalog code `ERR-500-EMPTY-SELECTION` (see §7).
- `Origin == PreExisting` implies the UI "Select Elements" control
  renders disabled (FR-003); `Origin == ManualPick` implies it renders
  enabled (FR-004).

---

## 2. Instance Parameter Candidate Set

Deduplicated collection backing Dialog Box 1.

| Field | Type | Notes |
|---|---|---|
| `Candidates` | `IReadOnlyList<ParameterCandidate>` | See `ParameterCandidate` below. |
| `Binding` | constant `ParameterBinding.Instance` | FR-007. |

### `ParameterCandidate` (shared between Instance and Type sets)

| Field | Type | Notes |
|---|---|---|
| `Name` | `string` | Deduplication key (research.md §d). |
| `Binding` | `ParameterBinding` (enum: `Instance`, `Type`) | Determines which dialog it belongs to. |
| `SourceElementRefs` | `IReadOnlyList<ElementRef>` | At least 1 element of the `Selection Context` where the parameter was observed (union, not intersection — FR-007/FR-008); used for log traceability, not to require universal presence. |

**Invariants**:
- Each `Name` appears **exactly once** per `Binding` inside its
  respective set (FR-007/FR-009: deduplicated, atomic).
- A `ParameterCandidate` never represents a non-writable, non-text, or
  wrong-binding parameter — that filtering happens in
  `IParameterDiscoveryPort` (adapter), not here; Domain only models the
  already-filtered result.

---

## 3. Type Parameter Candidate Set

Identical shape to the Instance Parameter Candidate Set, with
`Binding = ParameterBinding.Type` (FR-008). Backs Dialog Box 2.
Selecting a candidate from this set implies the UI must show the inline
warning mandated by FR-014 (modeled as
`ReplacementOperation.RequiresWideBlastRadiusWarning`, see §5).

---

## 4. Shared Search Query

Live filter state shared by both dialogs.

| Field | Type | Notes |
|---|---|---|
| `Text` | `string` (may be `""`) | FR-011. |
| `MatchesInstanceSet` | `IReadOnlyList<ParameterCandidate>` | Subset of §2 whose `Name` contains `Text` (case-insensitive). |
| `MatchesTypeSet` | `IReadOnlyList<ParameterCandidate>` | Subset of §3, same criterion. |

**Invariants**:
- Recalculating `MatchesInstanceSet`/`MatchesTypeSet` is a pure
  in-memory operation (no I/O), fired on every `Text` change (FR-011:
  "live, as the user types").
- `MatchesInstanceSet.Count == 0` (or the other) is not an error: the UI
  must communicate it as "no results" in that dialog only (FR-012),
  without affecting the other dialog.

---

## 5. Replacement Operation

The execution target: chosen parameter + new value + resolved scope.

| Field | Type | Notes |
|---|---|---|
| `TargetParameter` | `ParameterCandidate` | Exactly one, from Instance or Type set (FR-013). |
| `NewValue` | `string` | Cannot be `null`/blank at execution (FR-016). |
| `RequiresWideBlastRadiusWarning` | `bool` (derived: `TargetParameter.Binding == Type`) | FR-014/SC-010. |
| `ExecutionScope` | `ExecutionScope` (discriminated: `InstanceScope(Selection Context)` \| `TypeScope(IReadOnlyList<ResolvedType>)`) | Instance path uses `Selection Context` as-is (FR-017); Type path resolves the affected type(s) from the scope, but the effect extends to *all* model elements of that type, not only the selected ones (FR-018). |

**Invariants**:
- `TargetParameter == null` blocks the transition to the replacement
  step (FR-013) → `ERR-500-NO-PARAMETER-SELECTED`.
- `string.IsNullOrWhiteSpace(NewValue)` blocks execution (FR-016) →
  `ERR-500-EMPTY-VALUE`.
- `RequiresWideBlastRadiusWarning == true` is purely informational:
  **does not** add a gate or extra confirmation step (FR-014 explicitly
  non-blocking).

---

## 6. Batch Execution Result

Outcome of executing a `Replacement Operation`. Models both paths
explicitly instead of a single generic "count", because their semantics
differ (FR-026).

```text
BatchExecutionResult
├── Path: ParameterBinding                      // Instance | Type
├── InstanceOutcome?  (only if Path == Instance)
│   ├── UpdatedCount: int
│   └── Skips: IReadOnlyList<ElementSkip>
└── TypeOutcome?      (only if Path == Type)
    ├── AffectedTypes: IReadOnlyList<ResolvedType>
    └── TotalElementsUpdated: int
```

### `ElementSkip`

| Field | Type | Notes |
|---|---|---|
| `Element` | `ElementRef` | |
| `Reason` | `SkipReason` (enum) | Values: `ParameterMissing`, `ParameterReadOnly`, `ParameterNotText`, `WorksharingOwnedByOther`, `ModelGroupMember`, `OtherSuppressedNativeDialog` — covers FR-020, FR-024, FR-025, and the generic suppressed-dialog edge case. |
| `Code` | `WarningCode` (reference to §7) | Each `SkipReason` maps 1:1 to a 400 catalog code (FR-033). |
| `Message` | `string` | Corresponding non-technical message (FR-028). |

**Invariants**:
- `InstanceOutcome` and `TypeOutcome` are mutually exclusive according
  to `Path` — never both present (one `Replacement Operation` runs one
  path, FR-017 vs FR-018).
- If the global operation could not run at all (document not
  modifiable, transaction not startable), `BatchExecutionResult` is not
  built: a session-level `ERR-500-*` is produced instead and no element
  is considered touched (FR-019/SC-005) — see the `Blocked` transition
  in §9.
- `TypeOutcome.TotalElementsUpdated` counts **all** model elements of
  that type, including Model Group members (FR-018 explicit note in
  spec.md) — unlike `InstanceOutcome`, where Model Group members appear
  as `ElementSkip` with `Reason = ModelGroupMember`.

---

## 7. Error/Warning Code Catalog

Static catalog (not persisted as a stateful "entity", but as reference
data embedded in `Domain.ErrorCatalog`), with one entry per `spec.md`
edge case.

| `Code` | Severity | Trigger (FR / Edge Case) | Non-technical message |
|---|---|---|---|
| `ERR-500-EMPTY-SELECTION` | 500 | FR-006, edge case "Empty element selection" | "No elements are selected. Select one or more elements before continuing." |
| `ERR-500-NO-PARAMETER-SELECTED` | 500 | FR-013, matching edge case | "Choose a parameter from Dialog Box 1 or Dialog Box 2 before continuing." |
| `ERR-500-EMPTY-VALUE` | 500 | FR-016 | "Enter a parameter and a replacement value before running the update." |
| `ERR-500-DOCUMENT-NOT-MODIFIABLE` | 500 | edge case "active document cannot be modified" | "The model cannot be modified right now. No changes were made." |
| `ERR-500-NO-ACTIVE-DOCUMENT` | 500 | edge case "no active Revit document" | "Open a model in Revit before running this tool." |
| `WARN-400-PARAM-MISSING` | 400 | FR-020 | "This element does not have the selected parameter." |
| `WARN-400-PARAM-READONLY` | 400 | FR-020 | "This parameter cannot be edited on this element." |
| `WARN-400-PARAM-NOT-TEXT` | 400 | FR-020 | "This parameter does not hold text and cannot be updated by this tool." |
| `WARN-400-WORKSHARE-OWNED` | 400 | FR-024 | "This element is currently being edited by another user and was skipped." |
| `WARN-400-MODEL-GROUP-MEMBER` | 400 | FR-025 | "This element belongs to a group and cannot be batch-updated here. Edit it from within the group in Revit, or ungroup it, and try again." |
| `WARN-400-NO-SEARCH-MATCH` | 400 (informational, no batch impact) | edge case "search matches no parameters" | "No parameters match your search." |
| `WARN-400-SESSION-RECORD-FAILED` | 400 | edge case "log/metrics cannot be written" | "The session record could not be saved. The update still completed." |

**Invariants**:
- Every `Code` has exactly one `Severity` (`400`/`500`) and one
  non-technical message — no condition left unclassified
  (FR-027/FR-028).
- The Type-path model-wide effect (spec.md, matching edge case) **has
  no entry** in this catalog — explicitly not classified as 400/500; it
  is expected behavior and is reported only in the summary and log (see
  `Batch Execution Result` → `TypeOutcome`).

---

## 8. Session Record, Session Log, Session Metrics Record

### Session Record

Identifier that ties a log and metrics of the same invocation.

| Field | Type | Notes |
|---|---|---|
| `RunId` | `string` (8 hex chars) | research.md §f. |
| `DocumentName` | `string` (sanitized) | research.md §f. |
| `SessionId` | `string` (derived: `revit-{RunId}-{DocumentName}`) | Logical session id in metrics records. On-disk pair is `{RunId}-{DocumentName}.txt` / `.json`. |
| `StartedAtUtc` | `DateTimeOffset` | FR-039. |

### Session Log

In-memory representation of the lines that will be persisted to the
`.txt` (FR-035–FR-037); `Adapters.Persistence` flushes them to disk via
`ILoggerPort` (see `contracts/ports.md`). Not modeled as a full-session
in-memory list — each line is appended and drained immediately
(research.md §e) so history is not lost on an abnormal shutdown.

### Session Metrics Record

Models the NDJSON lines (FR-039–FR-043). Defined as a union of record
types, all with `SessionId` and `TimestampUtc`:

```text
MetricsRecord (abstract)
├── SessionStart        { }
├── SearchPerformed      { QueryText, MatchedInInstanceSet: string[], MatchedInTypeSet: string[] }
├── ParameterSelected    { Name, Binding }
├── PhaseTiming          { Phase: "Discovery" | "Execution", ElapsedMs: long }
├── BatchResult          { Path, UpdatedCount, SkippedCounts: Dictionary<SkipReason,int>,
│                          CountsByCategory: Dictionary<string, OutcomeCounts> }
│                          // OutcomeCounts = { Success: int, Warning: int, Error: int } — FR-042
└── SessionEnd           { FinalState: SessionState }
```

**Invariants**:
- Each NDJSON line is a complete, valid JSON object on its own
  (FR-043) — never a partial line or a multi-line array.
- `BatchResult.CountsByCategory` is always grouped both by
  classification type (success/warning/error) and by Revit element
  category (FR-042); the category key is the `CategoryName` captured on
  `ElementRef` (§1), not a value derived at report time.

---

## 9. Session (lifecycle / states)

Although `spec.md` does not name a "Session" entity beyond "Session
Record", the traceability FRs (FR-034) and the transitions in User
Stories 0–5 do define an implicit session lifecycle, formalized here as
a Domain state machine because it disciplines `Application` orchestration
and prevents invalid transitions (e.g. execute with no chosen parameter).

```text
Started
  → Discovering                (valid Selection Context, FR-006 satisfied)
      → AwaitingReplacementValue  (TargetParameter chosen, FR-013 satisfied)
          → Executing              (NewValue non-empty, FR-016 satisfied)
              → Completed          (BatchExecutionResult produced, full or partial)
              → Blocked            (transaction could not start/complete —
                                     FR-019/SC-005, no element modified)
  → Cancelled                   (user cancels at any point before Completed/Blocked)
```

**Transition invariants**:
- `Started → Discovering` requires `SelectionContext.IsValid == true`;
  otherwise the session stays in `Started` and
  `ERR-500-EMPTY-SELECTION` is emitted.
- `Discovering → AwaitingReplacementValue` requires
  `ReplacementOperation.TargetParameter != null`.
- `AwaitingReplacementValue → Executing` requires
  `!string.IsNullOrWhiteSpace(ReplacementOperation.NewValue)`.
- Any state may transition to `Cancelled` (User Story 2, scenario 3:
  cancel the manual pick without choosing elements).
- `Completed` and `Blocked` are terminal; both trigger writing
  `SessionEnd` on the `Session Metrics Record` and closing the
  `Session Log` (FR-034: "from launch through summary or cancellation").

Launch itself (User Story 0) is outside this machine: ribbon registration
happens in `App.OnStartup` before any session exists.

---

## 10. Installer Package

Modeled lightly because it does not participate in add-in runtime — it
is packaging metadata consumed by `BatchParamUpdate.Installer`
(research.md §h).

| Field | Type | Notes |
|---|---|---|
| `SupportedRevitYears` | `IReadOnlyList<int>` (`{2025, 2026, 2027}`) | FR-045/FR-046/FR-048. |
| `DetectedRevitYears` | `IReadOnlyList<int>` | Populated at `Installer` runtime via registry detection (research.md §h). |
| `Actions` | `enum { Install, Update, Uninstall }` per detected year | FR-047. |

**Invariants**:
- `SupportedRevitYears` is a closed list; the `Installer` never offers
  install for a year outside this list, nor claims unverified support
  (FR-046, SC-009).

---

## 11. Ribbon bootstrap (adapter-only — not a Domain entity)

`RibbonPanel`, `PushButton`, and graphic assets are Revit UI host
objects created by `App` (`IExternalApplication`) in `Adapters.Revit`.
They are listed in spec.md Key Entities for the evaluator, but they are
**not** modeled in `Domain` and they have **no port**: a single
production implementation with no domain-testable rules would be YAGNI.

| Host object | Notes |
|---|---|
| `App` | `IExternalApplication`; `.addin` `Application` class. |
| `RibbonPanel` | Custom panel created in `OnStartup` (FR-049/FR-050). |
| `PushButton` | Dedicated button launching `IExternalCommand` (FR-049). |
| Graphic assets | PNGs under `src/BatchParamUpdate.Adapters.Revit/Resources/` (64/100 provided; 16/32 derived if needed) (FR-051). |

---

## Relationships (summary)

```text
Selection Context 1───1 Session (initial state)
Selection Context 1───* ParameterCandidate (via Instance/Type Candidate Set)
Shared Search Query *───1 Instance Parameter Candidate Set
Shared Search Query *───1 Type Parameter Candidate Set
Replacement Operation 1───1 ParameterCandidate (TargetParameter)
Replacement Operation 1───0..1 Batch Execution Result
Batch Execution Result 1───* ElementSkip
ElementSkip *───1 Error/Warning Code Catalog (Code)
Session Record 1───1 Session Log
Session Record 1───* Session Metrics Record (NDJSON lines)
Installer Package 0..1 (independent of Session lifecycle)
App / RibbonPanel / PushButton  (adapter-only; not related to Domain entities)
```
