# Quickstart: End-to-end validation of the Batch Parameter Update Add-in

**Feature**: `001-batch-parameter-update` | **Spec**: [spec.md](./spec.md)
**References**: [data-model.md](./data-model.md) · [contracts/ports.md](./contracts/ports.md)

This guide does not repeat the data design or the contracts — it
references them. It includes no implementation code. Each step states a
verifier-checkable expected result, mapped to the User Story (US) and/or
Success Criteria (SC) in `spec.md` that it validates.

## Prerequisites

1. **Revit installed**: Autodesk Revit 2025, 2026, or 2027 (at least one
   of the three supported versions — FR-048). Note the exact version
   and, if applicable, the update level (e.g. "2026.5"), because it
   affects the expected real TFM (see `research.md` §a, risk #1).
2. **Test model** (`.rvt`) that contains, at minimum:
   - Elements with a writable **Instance**-bound text parameter shared
     by at least two elements of different categories (to populate
     Dialog Box 1 with variety).
   - Elements with a writable **Type**-bound text parameter on at least
     one family (to populate Dialog Box 2).
   - At least one element whose target parameter is read-only or missing,
     to validate the per-element skip (FR-020).
   - Ideally: an element that is a **Model Group** member with the
     target Instance parameter (for US4, Model Group scenario), and a
     **worksharing**-enabled copy of the model (central + local, or
     Collaboration for Revit) with an element seen from another session
     as "owned by other user" (for US4, worksharing scenario). If real
     worksharing cannot be simulated in the evaluation environment, that
     scenario may be validated by reading the code instead and left
     annotated as pending manual verification — it does not block the
     rest of the quickstart.
3. **Build + installer**: `BatchParamUpdate.sln` built Release for the
   available Revit year (see `plan.md` → Project Structure for per-year
   configurations), and Velopack installer generated (`vpk pack`, see
   `research.md` §h).

## Installation

1. Run the `Setup.exe` produced by Velopack (or the already-installed
   `Installer.exe` if this is an update).
   **Expected**: a WPF window opens listing Revit versions detected on
   the machine (FR-047).
2. Select the target Revit version and click "Install".
   **Expected**: the add-in assembly and its `.addin` manifest
   (`Application` class = `App`) are copied into that version's add-ins
   folder (see `research.md` §h for the Revit 2027-specific path, which
   differs from 2025/2026); no error appears.
3. Open Revit (the installed version) and confirm a **custom ribbon
   panel** with a **dedicated push-button** for this add-in (lineal-color
   optimization artwork). Launch **from that button**, not from a
   generic Add-Ins tab dump.
   **Validates**: SC-008, SC-014, FR-049–FR-051, US0.

## Scenario 1 — Pre-existing selection (US1)

1. In Revit, manually select 3–5 elements in the test model (mix of
   categories, at least one with the target Instance parameter as
   writable text).
2. Launch the add-in **from the dedicated ribbon button**.
   **Expected**: the window opens directly on the parameter-discovery
   view (Dialog Box 1 + Dialog Box 2 visible together); the "Select
   Elements" control appears disabled/greyed out.
   **Validates**: US1 scenario 1, FR-001–FR-003, SC-001.

## Scenario 2 — Manual selection from the add-in (US2)

1. In Revit, deselect everything (Esc) and launch the add-in from the
   dedicated ribbon button.
   **Expected**: the "Select Elements" control appears enabled; the UI
   clearly indicates no elements are in scope yet.
   **Validates**: FR-004, US2 scenario 1.
2. Click "Select Elements", pick one or more elements in the model, and
   confirm.
   **Expected**: those elements become the session scope; Dialog Box 1/2
   populate from them.
   **Validates**: FR-005, US2 scenario 2, SC-002.
3. Relaunch, click "Select Elements", and cancel the pick without
   choosing anything.
   **Expected**: the UI returns to a state with no valid scope and
   communicates that a selection is required to continue; advancing is
   not possible.
   **Validates**: FR-006, US2 scenario 3.

## Scenario 3 — Simultaneous discovery and shared search (US3)

1. With a valid scope established (Scenario 1 or 2), observe the window.
   **Expected**: Dialog Box 1 (Instance) and Dialog Box 2 (Type) are
   visible at the same time, with no need to switch tab/screen.
   **Validates**: FR-010.
2. Type progressively into the single search field a substring of a
   parameter name you know exists in the model (e.g. the first 3
   letters).
   **Expected**: both lists filter live, simultaneously, without
   clicking any "search" button.
   **Validates**: FR-011, SC-006.
3. Type a string that matches no known parameter.
   **Expected**: each dialog shows its own "no results" message (not an
   unexplained empty list).
   **Validates**: FR-012, edge case "search matches no parameters".
4. Select a parameter from Dialog Box 1.
   **Expected**: continuing to the replacement step is enabled; no
   warning appears (it is Instance).
   **Validates**: FR-013 (Instance branch).
5. Restart the flow and this time select a parameter from Dialog Box 2.
   **Expected**: an **inline, non-modal** warning appears immediately
   in the same window, stating that the change will affect every model
   element sharing that type — and you can continue without
   dismissing/accepting anything extra.
   **Validates**: FR-014, US3 scenario 5, SC-010.
6. Try to advance without having chosen any parameter in either dialog.
   **Expected**: the flow blocks with a message asking to choose a
   parameter from Dialog Box 1 or 2.
   **Validates**: FR-013, `ERR-500-NO-PARAMETER-SELECTED` (`data-model.md` §7).

## Scenario 4a — Instance-path execution with mixed outcomes (US4)

1. With an Instance parameter chosen (Scenario 3.4), leave the
   replacement-value field empty and try to confirm.
   **Expected**: rejected with a message asking for a replacement value.
   **Validates**: FR-016, `ERR-500-EMPTY-VALUE`.
2. Enter a valid replacement value and confirm, with a scope that mixes:
   elements where the parameter is writable, one without the parameter,
   one read-only, and (if available) one Model Group member and one
   workshared owned-by-other.
   **Expected during execution**: an **inline** progress indicator in
   the same window (never a separate popup); no native Revit dialog
   appears at any time (neither worksharing nor any other kind).
   **Validates**: FR-022, FR-023/FR-024, SC-011, SC-012.
3. When finished, review the summary.
   **Expected**: count of updated and skipped elements, each skip with a
   understandable non-technical reason (see `SkipReason` in
   `data-model.md` §6): missing parameter, read-only, non-text, owned by
   another user, group member — according to what was simulated.
   **Validates**: FR-021, FR-026, SC-003, SC-004, SC-013 (if the Model
   Group scenario was included).

## Scenario 4b — Type-path execution (US4)

1. Choose a Type parameter (Scenario 3.5), proceed past the inline
   warning (no extra action required), enter a replacement value, and
   confirm.
   **Expected**: the final summary states the affected type(s) and the
   total count of elements updated **across the whole model**, not only
   the selected ones.
   **Validates**: FR-018, FR-026 (Type branch), edge case "Type-level
   update... not classified as 400/500".
2. If the test model has an element of that type inside a Model Group,
   confirm after execution that that element **also** reflects the new
   value (unlike the Instance path).
   **Expected**: the grouped element is updated as well, because the
   Type-path writes the shared type object, not the grouped instance —
   this is the risk/assumption to re-verify in `research.md` (Risks
   summary, item 2); if observed behavior differs, document it and
   escalate before treating implementation as done.

## Scenario 5 — Globally blocked operation (US4, edge case)

1. Repeat Scenario 4a/4b against a non-modifiable document (e.g. opened
   read-only, or a disconnected central without write permission,
   according to what the evaluation environment can simulate).
   **Expected**: no element is left partially modified; a clear,
   non-technical message explains why the operation could not run.
   **Validates**: FR-019, edge case "document cannot be modified", SC-005.

## Scenario 6 — Log and metrics verification (US5)

After running at least one of the previous scenarios:

1. Locate, under `%TEMP%\juanManriqueHexagon\LOGS\`, the
   `revit-{runId}-{documentName}.txt` file for the session just run (see
   `data-model.md` §8 for the name format).
   **Expected**: the file exists, is readable as plain text, and
   contains entries for scope establishment, searches performed,
   chosen parameter/value, each individual skip (with its code), and
   the final summary.
   **Validates**: FR-035–FR-038.
2. Locate the sibling `.ndjson` file under
   `%TEMP%\juanManriqueHexagon\TRACKER\`.
   **Expected**: each line is an independent, valid JSON object (can be
   verified with any line-oriented tool, e.g.
   `Get-Content file.ndjson | ForEach-Object { $_ | ConvertFrom-Json }`
   in PowerShell with no line failing to parse). The content lets you
   determine without opening Revit: search-phase and execution-phase
   timings, whether the Instance or Type path was used, and outcome
   counts grouped by classification type and by element category.
   **Validates**: FR-039–FR-043, SC-007.
3. Confirm that the base name (`revit-{runId}-{documentName}`) is
   identical between the `.txt` and `.ndjson` of the same session.
   **Validates**: FR-038 (pairing artifacts by name).

## Overall expected result

If the 6 scenarios above complete with the expected results (or are
explicitly annotated as non-simulable in the evaluation environment,
without contradicting documented behavior), feature
`001-batch-parameter-update` is validated end-to-end and ready for
implementation work against this plan's design.
