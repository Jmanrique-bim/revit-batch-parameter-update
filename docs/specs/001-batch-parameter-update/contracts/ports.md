# Contracts: Domain/Application Ports

**Feature**: `001-batch-parameter-update` | **Spec**: [spec.md](../spec.md) | **Data model**: [data-model.md](../data-model.md)

This project does not expose a REST/web API: as a desktop add-in with
hexagonal architecture, its real "contracts" are the **ports**
(interfaces) that `Domain`/`Application` declare and that `Adapters`
implement. This document covers the 7 mandated ports. Each one states:
responsibility, key method signatures (high-level C# / pseudocode, no
implementation), and which concrete adapter implements it in production
vs. in tests.

All referenced types (`ElementRef`, `ParameterCandidate`,
`ReplacementOperation`, `BatchExecutionResult`, `MetricsRecord`, etc.)
are formalized in [`data-model.md`](../data-model.md). No method
signature here references `RevitAPI.dll` — that is exactly the guarantee
this port catalog exists to enforce (Testability pillar).

**Ribbon is not a port.** `App` (`IExternalApplication`), `RibbonPanel`,
and `PushButton` live only in `Adapters.Revit` (research.md §i,
data-model.md §11). An `IRibbonHost` would be a single-implementation
abstraction with no domain-testable behavior (YAGNI).

---

## 1. `IElementSelectionPort`

**Responsibility**: Resolve the current `Selection Context` when the
add-in launches, and allow the user to pick elements manually when there
is no prior selection.

```csharp
public interface IElementSelectionPort
{
    // FR-001/FR-002: selection already present when the command launches.
    SelectionContext GetPreExistingSelection();

    // FR-005: interactive user pick inside the active model.
    // Returns null if the user cancels the pick (User Story 2, scenario 3).
    SelectionContext? PromptManualSelection();
}
```

- **Production adapter**: `Adapters.Revit.Selection.RevitElementSelectionPort`
  — uses `UIDocument.Selection.GetElementIds()` for the pre-existing
  selection and `UIDocument.Selection.PickObjects(...)` for the manual
  pick.
- **Test adapter**: in-memory fake that returns a preconfigured
  `SelectionContext` or `null`, with no UI interaction.

---

## 2. `IParameterDiscoveryPort`

**Responsibility**: Given a `Selection Context`, discover
`ParameterCandidate`s of Instance and Type binding present on at least
one element/type in scope (deduplicated union — research.md §d).

```csharp
public interface IParameterDiscoveryPort
{
    // FR-007: Instance-bound, text, writable candidates.
    InstanceParameterCandidateSet DiscoverInstanceCandidates(SelectionContext scope);

    // FR-008: Type-bound, text, writable candidates, resolved from
    // each in-scope element's type.
    TypeParameterCandidateSet DiscoverTypeCandidates(SelectionContext scope);
}
```

- **Production adapter**: `Adapters.Revit.Discovery.RevitParameterDiscoveryPort`
  — implements the `StorageType.String && !IsReadOnly` filter described
  in research.md §d, iterating `Element.Parameters` and
  `document.GetElement(element.GetTypeId()).Parameters`.
- **Test adapter**: fake that returns pre-built sets from synthetic data
  (no real Revit `ElementId`).

---

## 3. `IParameterWritePort`

**Responsibility**: Execute the real replacement-value write, Instance
or Type, inside a single transaction, applying the proactive gates
(Model Group, worksharing) before each individual write.

```csharp
public interface IParameterWritePort
{
    // FR-017/FR-019/FR-020/FR-021/FR-025: Instance path.
    // Never throws for an individual element: each failure becomes an
    // ElementSkip inside the returned result.
    BatchExecutionResult ExecuteInstanceUpdate(
        SelectionContext scope,
        ParameterCandidate targetParameter,
        string newValue);

    // FR-018: Type path — writes the affected ElementType(s);
    // the updated-element count is model-wide, not limited to the
    // original scope.
    BatchExecutionResult ExecuteTypeUpdate(
        SelectionContext scope,
        ParameterCandidate targetParameter,
        string newValue);
}
```

- **Production adapter**: `Adapters.Revit.Writing.RevitParameterWritePort`
  — opens a single `Transaction`, registers the `IFailuresPreprocessor`
  from research.md §b, consults `INativeDialogSuppressionPort` +
  `element.GroupId`/`WorksharingUtils` before each `Parameter.Set(...)`,
  and commits/rolls back according to whether the global operation could
  proceed (FR-019).
- **Test adapter**: fake that simulates writes on synthetic `ElementRef`s
  and produces configurable `ElementSkip`s per scenario, to test 400/500
  classification logic without real transactions.

---

## 4. `INativeDialogSuppressionPort`

**Responsibility**: Encapsulate the two-layer native-dialog suppression
from research.md §b (proactive worksharing check +
`DialogBoxShowing`/`IFailuresPreprocessor` safety net), exposed to
`Application` as a simple query operation, without leaking Revit API
details into Domain.

```csharp
public interface INativeDialogSuppressionPort
{
    // Proactive check (research.md §b, layer 1). Fires no dialog; it is
    // a read-only query of the element's worksharing state.
    WorkshareStatus GetWorkshareStatus(ElementRef element);

    // Enables/disables the safety net (layer 2) for the batch execution
    // window. IParameterWritePort wraps it around the full Transaction.
    IDisposable SuppressNativeDialogsDuringBatch();
}

public enum WorkshareStatus { NotWorkshared, OwnedByCurrentUser, OwnedByOtherUser }
```

- **Production adapter**: `Adapters.Revit.DialogSuppression.RevitDialogSuppressionPort`
  — `WorksharingUtils.GetCheckoutStatus`/`GetWorksharingTooltipInfo` for
  the query; `UIApplication.DialogBoxShowing` +
  `IFailuresPreprocessor` registration, with matching
  `IDisposable.Dispose()` to unsubscribe when the batch ends.
- **Test adapter**: fake that returns a fixed `WorkshareStatus` per
  `ElementRef` and a no-op `IDisposable`.

---

## 5. `ILoggerPort`

**Responsibility**: Single write point for `Session Log` (`.txt`) lines,
consumed by `Domain`/`Application` (research.md §e).

```csharp
public interface ILoggerPort
{
    void Info(string message);
    void Warn(string message, WarningCode code);
    void Error(string message, ErrorCode code);

    // Closes/drains this session's write buffer (FR-034: the session
    // ends in Completed/Blocked/Cancelled).
    void CloseSession();
}
```

- **Production adapter**: `Core.SessionFileLogger` (single canonical
  implementation, research.md §e), consumed by `Adapters.Persistence`
  and exposed to `Domain`/`Application` through this port.
- **Test adapter**: in-memory fake (`List<string>`) for content
  assertions without touching the filesystem.

---

## 6. `ISessionRecorderPort` (NDJSON metrics)

**Responsibility**: Persist each `MetricsRecord` (data-model.md §8) as
an independent NDJSON line, without coupling `Domain`/`Application` to
the concrete JSON serialization format.

```csharp
public interface ISessionRecorderPort
{
    void Record(MetricsRecord record); // FR-039–FR-043: one append per line.
}
```

- **Production adapter**: `Adapters.Persistence.NdjsonSessionRecorder`
  — serializes `MetricsRecord` (via `System.Text.Json`) and
  `File.AppendAllText`s to
  `%TEMP%\juanManriqueHexagon\TRACKER\revit-{runId}-{documentName}.ndjson`
  (research.md §f). If the write fails (permissions), it catches the
  exception, emits `WARN-400-SESSION-RECORD-FAILED` via `ILoggerPort`,
  and **does not** propagate the failure to the in-flight batch
  (matching spec.md edge case: "does not block the batch operation
  itself").
- **Test adapter**: fake that accumulates received `MetricsRecord`s in a
  list for assertions.

---

## 7. `IInstallerPort`

**Responsibility**: Isolate, inside the `Installer` project, detection
of installed Revit versions and per-year install/update/uninstall
actions, so the installer UI (WPF) does not contain Windows-registry or
file-copy logic directly.

```csharp
public interface IInstallerPort
{
    IReadOnlyList<int> DetectInstalledRevitYears();   // FR-047

    void Install(int revitYear);
    void Update(int revitYear);
    void Uninstall(int revitYear);
}
```

- **Production adapter**: `Installer.RevitInstallerAdapter` — reads
  `HKEY_LOCAL_MACHINE\SOFTWARE\Autodesk\Revit\{year}` (research.md §h)
  and copies the `Adapters.Revit` assembly built for that year plus its
  `.addin` manifest (`Application` = `App`) to the matching add-ins
  folder (with the Revit 2027 caveat documented as a risk in
  research.md §h).
- **Test adapter**: fake that simulates a fixed list of "installed"
  years and records received `Install`/`Update`/`Uninstall` calls.

---

## Summary: who implements what

| Port | Production adapter | Test adapter |
|---|---|---|
| `IElementSelectionPort` | `Adapters.Revit.Selection` | In-memory fake |
| `IParameterDiscoveryPort` | `Adapters.Revit.Discovery` | In-memory fake |
| `IParameterWritePort` | `Adapters.Revit.Writing` | In-memory fake |
| `INativeDialogSuppressionPort` | `Adapters.Revit.DialogSuppression` | In-memory fake |
| `ILoggerPort` | `Core.SessionFileLogger` (via `Adapters.Persistence`) | In-memory fake (`List<string>`) |
| `ISessionRecorderPort` | `Adapters.Persistence.NdjsonSessionRecorder` | In-memory fake |
| `IInstallerPort` | `Installer.RevitInstallerAdapter` | In-memory fake |

`Tests.Unit` (xUnit) references only `Domain`/`Application` and the
right-hand-column fakes — never production adapters nor `RevitAPI.dll`
(research.md §g). `App` / ribbon / icons are exercised only in the
manual `quickstart.md` path (SC-014).
