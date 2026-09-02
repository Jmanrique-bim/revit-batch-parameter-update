# Testing

## Automated (no Revit, runs in CI)

```powershell
dotnet test tests/BatchParamUpdate.Tests.Unit/BatchParamUpdate.Tests.Unit.csproj
```

Covers:

| Area | Test |
|---|---|
| Update-or-skip decision, every branch (missing / read-only / non-text / group / workshare / value-rejected / not-found / success) | `Domain/ParameterWriteDecisionTests` |
| Stable parameter identity (built-in id / shared GUID / name fallback) | `Domain/ParameterKeyTests` |
| Full flow through the coordinator: pre-selection, manual pick, empty-scope block (`EmptySelection`), mixed skips, transaction rollback, progress reporting, terminal state | `Application/BatchUpdateCoordinatorTests`, `Application/InstancePathUseCaseTests` |
| Events → `.txt` log + NDJSON metrics, aggregation, recorder-failure swallowed | `Application/SessionTraceListenerTests` |
| Hexagon holds — Domain/Application have no outward dependency | `Architecture/LayerDependencyTests` |
| 400/500 catalog completeness and non-technical wording | `Domain/ErrorWarningCatalogTests` |
| Installer path is per-user, needs no elevation; legacy all-users path resolves under `%ProgramData%` | `Domain/RevitAddinPathsTests` |

The Revit-facing adapters (`RevitParameterWritePort`, `RevitParameterDiscoveryPort`, `RevitElementSelectionPort`, `RevitDialogSuppressionPort`, `RevitWorksharingStatusPort`) are thin translations onto `ParameterWriteDecision` and the Domain ports; their pure logic is what the tests above exercise.

## Manual (requires Revit 2025 and Revit 2026)

Run each on both supported years. Use a model with a wall that has a writable `Comments`, a wall with a read-only text parameter, and a wall inside a Model Group.

1. **Pre-selection** — select the three walls, launch the add-in. The window is modeless (you can click in Revit behind it). It opens straight into the parameter panel (full width, no Type panel). Choose `Comments`, enter a value, **Run update**. The progress bar advances in real time during the write (input is queued until it finishes); summary reads *updated 1 / skipped 2* with reasons (read-only, belongs to a group). While the write runs, extra clicks on **Run update** / **Select Elements** do nothing — no second transaction, no error.
2. **Empty launch (User Story 2)** — launch with nothing selected. Window opens with **Select Elements** enabled. Try to advance without picking → explicit blocking message *"No elements are selected…"*.
3. **Value rejected** — target a parameter/value Revit refuses → the element appears under skipped as *"Revit did not accept the new value…"*, not under updated.
4. **Rollback** — force the transaction to roll back (e.g. a constraint failure elevated to error) → summary reads *"Revit rejected the changes. No elements were modified."*, no success count, but the per-element skip grid and CSV export still list the skips the run collected. NDJSON `CountsByCategory` records no `ok` entries for that run.
5. **Deleted element** — delete a selected element before **Run update** → skipped as *"This element no longer exists in the model."*
6. **Session artifacts** — after any run, `%LOCALAPPDATA%\juanManriqueHexagon\LOGS\{runId}-{doc}.txt` is readable and `\TRACKER\{runId}-{doc}.json` is NDJSON with phase timings, the search terms, the parameter/value, and outcome counts by category and severity.
7. **Installer** — `pack.ps1`, then run `Installer.exe` as a normal user (no "Run as administrator"). The add-in lands in `%APPDATA%\Autodesk\Revit\Addins\{year}` and loads on next Revit start. If an older build had placed a manifest under `%ProgramData%\Autodesk\Revit\Addins\{year}`, install/uninstall removes it (best-effort) so the ribbon button is not registered twice.

## Deferred: automated Revit integration project

A `tests/BatchParamUpdate.Tests.Revit` project (a `[Transaction]` xUnit harness plus a checked-in minimal `.rvt` fixture) would automate scenarios 1 and 3–5 against a real host. It is not in the repo yet because the fixture must be authored inside Revit; the manual matrix above is the current substitute and each supported year is exercised through it.
