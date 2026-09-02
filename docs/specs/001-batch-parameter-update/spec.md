# Feature Specification: Batch Parameter Update Revit Add-in

**Feature Branch**: `001-batch-parameter-update`
**Created**: 2026-08-31
**Status**: Draft
**Input**: Deliver a Revit add-in (WPF, C#/.NET, Revit API 2025-2026) that batch-updates a writable text parameter across a user-selected set of elements via two operational search-and-replace paths shown simultaneously in one UI — Instance-bound parameters (Dialog Box 1) and Type-bound parameters (Dialog Box 2), filtered by a single shared search — with a 400/500 error-code taxonomy, non-blocking inline warnings, automatic suppression of native Revit dialogs during batch execution, per-session logging and NDJSON metrics persisted under the system Temp folder, and a Velopack-based installer with an interactive install/uninstall/update UI — extending the base "Revit Add-in: Batch Parameter Update" technical assessment with stakeholder-mandated architecture, observability, and packaging requirements.

## Overview

This feature is a Revit add-in that lets a BIM user update the same text-based parameter across many model elements in one operation instead of editing each element by hand. It is aimed at BIM coordinators and modelers who routinely need to correct or standardize a text value (e.g., a status code, a comment, a classification tag) across a batch of elements they have identified in the model, either before or after opening the tool.

The base requirement — select elements, enter a parameter name and value, update matching writable text parameters, skip and report what could not be updated — comes from the original technical assessment brief, which restricts the required flow to a **writable text instance parameter**. This submission honors that restriction as the primary path (Dialog Box 1) and adds a second, symmetric path for **text parameters bound at the Type level** (Dialog Box 2), because Revit's parameter model splits every parameter into an Instance or a Type binding and the two require materially different update operations and carry different consequences: updating an Instance parameter only touches the elements the user selected, while updating a Type parameter changes the value for **every element in the model that shares that type** — not only the selected ones. Which operation runs is determined automatically by where the parameter the user picked is actually bound, not by a manual mode switch.

This expanded scope exists because the deliverable is evaluated not only on "does the batch update work" but on four pillars explicitly required by the stakeholder: **Functionality** (the update behaves correctly end-to-end, on either path), **Traceability** (every session's searches, parameters touched, and outcomes can be reconstructed after the fact), **Observability** (structured, classified success/warning/error reporting, not just pass/fail), and **Testability** (the business logic is exercised independently from the host application so it can be verified without a full Revit session). The underlying stack, architecture, and tooling decisions that support it are documented as constraints in this spec's Assumptions section rather than as open questions.

Both dialogs operate over the exact same current element selection scope, are displayed **simultaneously in the same UI** (not as sequential screens the user navigates between), and share a **single search input**: one query the user types filters and updates both Dialog Box 1's and Dialog Box 2's lists live, at the same time. Parameter discovery for each dialog is a **deduplicated union**, not an intersection: a parameter is listed — once, regardless of how many selected elements share it — as soon as it exists, with the correct binding and text type, on **at least one** element in the current selection scope. It does not need to exist on every selected element. Elements in scope that lack the chosen parameter at execution time are skipped individually and the skip is recorded; this is the expected, common outcome of union-based discovery, not an exceptional case. The user must choose exactly one parameter — from either box — before the flow can proceed; choosing a Type-bound parameter surfaces an inline, non-blocking warning directly in the UI rather than a separate confirmation dialog, and does not stop the user from continuing.

An earlier draft of this spec also included a third, purely informational dialog that surfaced non-text parameters common to the selection as a diagnostic aid. That informational view is explicitly **out of the current scope** and deferred to a future version — see "Future Enhancements (Deferred to V2)" below.

## User Scenarios & Testing *(mandatory)*

### User Story 0 - Launch the add-in from a dedicated ribbon button (Priority: P1)

A user opens Revit and starts the batch parameter update from a dedicated push-button on a custom ribbon panel registered by the add-in at startup — not from a generic dump of commands on the Add-Ins tab. The button uses the assignment's lineal-color optimization artwork.

**Why this priority**: Without a discoverable host entry point, none of the later stories can be reached in a real Revit session.

**Independent Test**: Install the add-in, start a supported Revit year, and confirm a custom panel with a dedicated push-button is present; clicking it launches the batch parameter update command.

**Acceptance Scenarios**:

1. **Given** the add-in is installed for a supported Revit year, **When** Revit starts, **Then** an `IExternalApplication` (`App`) registers a custom ribbon panel and a dedicated push-button that targets the batch parameter update `IExternalCommand`.
2. **Given** Revit is open with the add-in loaded, **When** the user looks at the ribbon, **Then** the command is available from that dedicated panel and button, not only as an undifferentiated Add-Ins tab listing.
3. **Given** the ribbon button is visible, **When** the user inspects its artwork, **Then** it uses the provided lineal-color optimization icons copied into the add-in as graphic resources (colors taken from that asset; 16px/32px ribbon sizes derived at implementation if the source files are larger).

### User Story 1 - Update parameters on an already-selected set of elements (Priority: P1)

A user has already selected one or more elements in the active Revit model (e.g., via a filter, a view selection, or a previous pick) and launches the add-in from the dedicated ribbon button to correct a text parameter across all of them in one pass.

**Why this priority**: This is the core value proposition from the original brief and the most common expected usage pattern; without it, the feature delivers no value.

**Independent Test**: Can be fully tested by selecting a mixed set of elements in Revit, launching the command from the dedicated ribbon button, and verifying the add-in immediately recognizes the existing selection (without requiring the user to reselect) and proceeds to parameter discovery.

**Acceptance Scenarios**:

1. **Given** one or more elements are already selected in the active Revit document, **When** the user launches the add-in command from the dedicated ribbon button, **Then** the add-in opens directly into the parameter-selection experience using the pre-existing selection, and the manual "Select Elements" control is shown disabled/greyed out.
2. **Given** the add-in opened using a pre-existing selection, **When** the user completes the parameter and value entry and confirms, **Then** the batch update runs only against the elements that were selected at launch time (no additional elements are silently included), except for the model-wide effect inherent to a Type-level update as documented in User Story 4.

### User Story 2 - Select elements manually from inside the add-in (Priority: P1)

A user launches the add-in without having selected anything in Revit first, and needs a way to pick the target elements from within the tool itself.

**Why this priority**: Equally core to the required flow ("two possible flows" mandated by the stakeholder); without it, users who forget to preselect are blocked entirely.

**Independent Test**: Can be fully tested by launching the add-in with an empty Revit selection and verifying the "Select Elements" control becomes enabled and, once used, populates the tool with the elements the user picked in the model.

**Acceptance Scenarios**:

1. **Given** no elements are selected in the active Revit document, **When** the user launches the add-in command, **Then** the "Select Elements" control is enabled and the tool clearly indicates no elements are currently in scope.
2. **Given** the "Select Elements" control is enabled, **When** the user picks one or more elements in the model and confirms the pick, **Then** those elements become the active selection scope for the rest of the session, and parameter discovery proceeds using that scope.
3. **Given** the user is offered manual selection, **When** the user cancels the pick without choosing any elements, **Then** the add-in returns to a state where no batch can be executed and clearly communicates that an element selection is required.

### User Story 3 - Discover and choose the target parameter from two simultaneous, jointly-searchable dialogs (Priority: P1)

A user needs to find the exact text parameter to update. Dialog Box 1 and Dialog Box 2 are both visible together in the same window — the user never has to open, close, or switch between them — and both operate over the identical current element selection scope using deduplicated-union discovery: a parameter appears once as soon as it exists on at least one element in scope, not only when it exists on all of them. Dialog Box 1 lists writable, **Instance-bound** text parameters found on at least one element in scope; Dialog Box 2 lists writable, **Type-bound** text parameters found on at least one selected element's type. Which box a parameter appears in is determined by how Revit binds that parameter for the relevant family, resolved automatically by the system. A single search input drives both lists at once: as the user types one query, Dialog Box 1 and Dialog Box 2 both filter and refresh live, in real time.

**Why this priority**: This is the mandated two-path discovery mechanism that replaces a plain free-text parameter name field from the base brief; it is required for the tool to be usable on models with many families and parameters, and it is a P1 because the replacement step (Story 4) depends on a parameter having been chosen here.

**Independent Test**: Can be tested by opening the add-in against a selection whose elements expose several text parameters — some shared by only a subset of the selection, at both Instance and Type binding — and confirming both dialogs are visible at the same time, that typing one query updates both lists simultaneously, that each dialog lists every distinct parameter of its respective binding exactly once (including parameters present on only one of the selected elements), and that the flow cannot advance until exactly one parameter has been picked from either dialog.

**Acceptance Scenarios**:

1. **Given** a valid element selection scope has been established (Story 1 or Story 2), **When** the add-in proceeds past selection, **Then** Dialog Box 1 and Dialog Box 2 are both displayed together in the same window, with Dialog Box 1 showing every distinct writable, Instance-bound, text-typed parameter present on **at least one** element in scope (each shown exactly once) and Dialog Box 2 showing every distinct writable, Type-bound, text-typed parameter present on **at least one** selected element's type (each shown exactly once).
2. **Given** both dialogs are displayed, **When** the user types characters into the single shared search input, **Then** the displayed lists in **both** Dialog Box 1 and Dialog Box 2 update live and simultaneously to reflect matches, without requiring a submit action and without the user needing to repeat the query in a second search box.
3. **Given** the shared search returns no matches for the current query in a given dialog, **When** the user views that dialog's results area, **Then** the tool clearly communicates that no parameters matched the query in that dialog rather than showing an empty, unexplained list.
4. **Given** the user selects a parameter from Dialog Box 1, **When** they confirm the choice, **Then** the tool identifies that parameter as an **Instance-level** target and enables proceeding to the replacement step.
5. **Given** the user instead selects a parameter from Dialog Box 2, **When** they confirm the choice, **Then** the tool identifies that parameter as a **Type-level** target, immediately displays an inline, non-blocking warning within the same UI stating that applying this update will affect every element in the model that shares that type — not only the elements in the current selection — and still allows the user to proceed directly to the replacement step without dismissing any additional dialog.
6. **Given** neither dialog has a selected parameter, **When** the user attempts to proceed to the replacement step, **Then** the tool blocks the transition and communicates that a parameter must be chosen from Dialog Box 1 or Dialog Box 2 before continuing.

### User Story 4 - Enter the replacement value and run the batch update (Priority: P1)

Once a target parameter is chosen — either an Instance-level parameter from Dialog Box 1 or a Type-level parameter from Dialog Box 2 — the user enters the new text value and runs the update, then reviews a summary of what happened. The operation the system performs differs by which path was used: an Instance-level update only ever touches elements in the current selection scope; a Type-level update touches every element in the model sharing the affected type, which was already disclosed inline at the moment the parameter was chosen (Story 3) and does not require any further confirmation here. While the batch executes, progress is shown inline in the same window, and any native Revit dialog that would otherwise interrupt the run per element (such as a workshared editing-permission request) is suppressed automatically so the user is not asked to approve anything element-by-element.

**Why this priority**: This is the operation that actually delivers the batch update value; it must run reliably on either path, without unnecessary interruptions, and report outcomes clearly, including partial success and, for the Type path, the wider effect.

**Independent Test**: For the Instance path, can be tested end-to-end by selecting a mix of elements where the target parameter is present and writable on some, missing on others, and read-only on others — including at least one element that is actively owned by another user in a workshared model and at least one element that is a member of a Revit Model Group — and confirming the update applies only where valid, the workshared element and the grouped element are both skipped without any native Revit dialog appearing, a progress indicator is visible inline throughout, and the rest are skipped and reported with reasons. For the Type path, can be tested by selecting elements of a given type, choosing a Type-bound parameter, and verifying after execution that every element of that type in the model — not just the selected ones, and regardless of whether some of them are members of a Model Group — reflects the new value.

**Acceptance Scenarios**:

1. **Given** an Instance-level target parameter chosen from Dialog Box 1, **When** the user enters a new text value and confirms, **Then** the add-in shows an inline progress indicator within the same window while it applies that value to the matching writable text Instance parameter on every element in the current selection scope for which the parameter is present, writable, and text-typed, inside a single reversible model transaction.
2. **Given** an element in the current selection scope does not have the Instance-level target parameter, or has it read-only, or has it as a non-text parameter, **When** the batch runs, **Then** that element is skipped, the rest of the batch continues uninterrupted, and the reason for skipping that element is recorded — this is the expected, common outcome of deduplicated-union discovery, since a parameter is never required to exist on every selected element to be offered in Dialog Box 1.
3. **Given** an element in the current selection scope is actively owned by another user in a workshared/cloud-shared model, **When** the batch reaches that element, **Then** the system suppresses any native Revit dialog that would normally request editing permission, skips that element, and records the reason — without requiring the user to dismiss or approve anything for that element.
4. **Given** an element in the current selection scope for an Instance-level update is a member of a Revit Model Group, **When** the batch reaches that element, **Then** the system does not attempt to modify it and does not ungroup or otherwise restructure the group, but instead skips that element and records the reason, checked proactively rather than discovered via a failed write attempt.
5. **Given** a Type-level target parameter chosen from Dialog Box 2 (with its inline warning already shown at selection time in Story 3), **When** the user enters a new text value and confirms, **Then** the add-in applies that value to the matching writable text Type parameter, updating every element in the model that shares the affected type — including elements that are members of a Model Group, since a Type-level update targets the shared Type definition rather than the grouped instance — inside a single reversible model transaction, without presenting any additional confirmation dialog.
6. **Given** the batch has finished running (fully or partially, via either path), **When** the summary is displayed, **Then** it shows, for the Instance path, the count of elements updated and skipped with reasons, or, for the Type path, the affected type(s) and the total count of elements updated across the model, together with the applicable classification code(s) and non-technical explanations.
7. **Given** the operation cannot proceed at all (e.g., the model cannot be modified), **When** the user attempts to confirm the update, **Then** no element is left partially modified and the user is shown a clear, non-technical explanation of why the operation could not run.

### User Story 5 - Recover what happened in a past session (Priority: P2)

A user, developer, or evaluator needs to confirm after the fact what a given add-in run searched for, which parameter and value were applied, whether the Instance or Type path was used, how long it took, and what the outcome was — without relying on memory or screenshots.

**Why this priority**: Required by the stakeholder's traceability/observability pillars; it is P2 because the tool is still usable end-to-end without it, but the submission is materially incomplete without session-level persistence.

**Independent Test**: Can be tested by running the add-in through a full update cycle on either path, then locating the session's persisted log and NDJSON metrics/tracker records on disk afterward — named using that session's unique identifier and the document name — and confirming they describe that session's searches, the path used, parameter/value, timing, and outcome, including counts grouped by outcome type and element category.

**Acceptance Scenarios**:

1. **Given** an add-in session has started (dialogs opened, searches performed), **When** the session ends (successfully, partially, or via cancellation), **Then** a session-scoped log recording the session's activity and a session-scoped JSON Lines metrics record of the parameters/values searched and the operation's summary (including which path was used and any classification codes) are both persisted to disk, each named with the same unique identifier (`{runId}-{documentName}`).
2. **Given** a completed session's persisted records, **When** they are inspected afterward, **Then** the timing of the search phase and the execution phase, whether the Instance or Type path was used, and the count of outcomes (updated/succeeded, warnings, errors) grouped by classification type and by element category can all be determined from the persisted data.

### Edge Cases

Each edge case below is classified as a **400 (warning)** — the batch can still make partial progress — or a **500 (error)** — the requested operation cannot proceed at all — and must present a non-technical message to the user, unless noted as explicitly not classified.

- Empty element selection when the user attempts to proceed past the selection step (no pre-existing selection and no manual pick made) → **500**: "No elements are selected. Select one or more elements before continuing."
- No parameter has been selected from either Dialog Box 1 or Dialog Box 2 when the user attempts to proceed, or the user cancels the flow before choosing one → **500**: "Choose a parameter from Dialog Box 1 or Dialog Box 2 before continuing."
- Empty or blank parameter/replacement value entered by the user → **500**: "Enter a parameter and a replacement value before running the update."
- Target Instance parameter is missing on a given element, or exists but is read-only, or exists but is not text-typed, at execution time → **400** (per-element skip; this is the expected, common outcome under deduplicated-union discovery, not an exceptional case): "This element does not have the selected parameter." / "This parameter cannot be edited on this element." / "This parameter does not hold text and cannot be updated by this tool."
- Selection mixes elements where the Instance-level target parameter is writable with elements where it is missing, read-only, or non-text → **400** (aggregate outcome, expected under union-based discovery): batch still runs; summary reports both updated and skipped counts.
- Search in the shared search input matches no parameters in Dialog Box 1, Dialog Box 2, or both → **400** (informational only, no batch impact): "No parameters match your search."
- An element in scope is part of a workshared/cloud-shared model and is actively owned/checked out by another user, so it cannot be edited → **400** (per-element skip, native Revit access-request dialog auto-suppressed so the user is never prompted to approve or deny per element): "This element is currently being edited by another user and was skipped."
- Any other native Revit dialog or warning that would normally require interactive, per-instance dismissal during a batch run appears → auto-suppressed by the system (no popup shown to the user); if the underlying action still cannot complete as a result, the affected element is skipped and the reason is recorded through the same **400** per-element mechanism above, rather than opening a second, distinct error path.
- An element targeted by an Instance-level update is a member of a Revit Model Group → **400** (per-element skip, checked proactively before attempting the write, not discovered by catching a failed update): Revit's API does not support modifying a grouped element's own parameters outside interactive "Edit Group" mode — forcing it either throws a hard failure when the group type has more than one instance in the model, or, for a lone instance, only "succeeds" by suppressing an unstable warning that the Revit developer community explicitly warns can corrupt or crash large models when applied across many elements. This tool does not attempt either path or an automatic ungroup/regroup: "This element belongs to a group and cannot be batch-updated here. Edit it from within the group in Revit, or ungroup it, and try again."
- A Type-level update runs successfully and, as designed, affects elements in the model that were not part of the original selection → **not classified as 400/500** (expected, by-design behavior, disclosed inline at parameter-selection time, not an error): the final summary and session log explicitly state the affected type(s) and the total element count updated, so this is never a silent side effect.
- The active document cannot be modified (e.g., read-only, or a transaction cannot be started) → **500**: "The model cannot be modified right now. No changes were made."
- The add-in is launched with no active Revit document open → **500**: "Open a model in Revit before running this tool."
- Session log or metrics/tracker record cannot be written to disk (e.g., permissions) → **400** (does not block the batch operation itself, but is itself reported): "The session record could not be saved. The update still completed."

## Requirements *(mandatory)*

### Functional Requirements

**Element Selection**

- **FR-001**: The system MUST detect, at add-in launch, whether one or more elements are already selected in the active Revit document.
- **FR-002**: The system MUST use a pre-existing selection as the element scope for the session without requiring the user to reselect, when one exists at launch.
- **FR-003**: The system MUST provide a "Select Elements" control that is disabled by default whenever a valid pre-existing selection is used as the scope.
- **FR-004**: The system MUST enable the "Select Elements" control whenever the add-in is launched with no elements selected in the active document.
- **FR-005**: The system MUST allow the user to pick one or more elements in the active model via the "Select Elements" control and adopt that pick as the element scope for the session.
- **FR-006**: The system MUST prevent the user from proceeding to parameter discovery while the element scope is empty, and MUST communicate that an element selection is required.

**Parameter Discovery & Selection — Dialog Box 1 (Instance) and Dialog Box 2 (Type)**

- **FR-007**: The system MUST, once an element scope is established, discover the set of writable, Instance-bound, text-typed parameters present on **at least one** element in the current selection scope — not only those present on every element in scope — and present each distinct parameter exactly once in Dialog Box 1 (atomic, deduplicated display).
- **FR-008**: The system MUST, from the SAME current element selection scope, discover the set of writable, Type-bound, text-typed parameters present on **at least one** selected element's type — not only those present on every selected element's type — and present each distinct parameter exactly once in Dialog Box 2 (atomic, deduplicated display).
- **FR-009**: The system MUST exclude from Dialog Box 1 any parameter that is not writable, not text-typed, or not bound at the Instance level; the system MUST exclude from Dialog Box 2 any parameter that is not writable, not text-typed, or not bound at the Type level.
- **FR-010**: The system MUST display Dialog Box 1 and Dialog Box 2 simultaneously within the same UI, so that the user can see and use both without opening, closing, or navigating between separate screens.
- **FR-011**: The system MUST provide a single, shared search input whose text simultaneously filters and updates the parameter lists displayed in both Dialog Box 1 and Dialog Box 2, live, as the user types, without requiring a submit action and without a separate search input per dialog.
- **FR-012**: The system MUST communicate clearly, within each dialog independently, when no parameters (matching that dialog's binding, or matching the current search text) are found.
- **FR-013**: The system MUST allow the user to select exactly one parameter, from either Dialog Box 1 or Dialog Box 2, as the target of the batch update, and MUST prevent the flow from proceeding to the replacement-value step until such a selection has been made.
- **FR-014**: The system MUST, immediately when the user selects a parameter from Dialog Box 2, display an inline, non-modal warning within the same UI stating that applying this parameter will affect every element in the model that shares that type — not only the elements in the current selection. This warning MUST NOT be a separate popup/dialog, and MUST NOT block or otherwise require an additional confirmation step before the user can proceed.

**Replacement & Batch Execution**

- **FR-015**: The system MUST, once a parameter has been selected from either Dialog Box 1 or Dialog Box 2, present an input for the user to enter the new text value to apply.
- **FR-016**: The system MUST reject an attempt to run the update when the replacement value is empty or blank, and MUST communicate why.
- **FR-017**: The system MUST, when the target was chosen from Dialog Box 1, apply the replacement value to the matching writable text Instance parameter on every element in the current selection scope for which the parameter is present, writable, and text-typed.
- **FR-018**: The system MUST, when the target was chosen from Dialog Box 2, apply the replacement value to the matching writable text Type parameter, which updates every element in the model sharing the affected type, without requiring any confirmation step beyond the inline warning already shown at selection time (FR-014).
- **FR-019**: The system MUST perform all model modifications for a single batch run inside one transaction, such that if the operation cannot proceed at all, no element is left partially modified.
- **FR-020**: The system MUST skip, without interrupting the rest of the batch, any element in the current selection scope for which an Instance-level target parameter is missing, read-only, or not text-typed.
- **FR-021**: The system MUST record, for every skipped element, a reason describing why it was skipped.
- **FR-022**: The system MUST display a progress indicator inline, within the same window used for the rest of the flow, while a batch is executing; this indicator MUST NOT be presented as a separate popup or dialog.
- **FR-023**: The system MUST intercept and automatically suppress native Revit dialogs that would otherwise require interactive, per-element approval during a batch run (including, but not limited to, workshared/cloud-model editing-permission requests), so the user is never required to dismiss or approve such a dialog once per affected element.
- **FR-024**: The system MUST treat an element that cannot be edited because it is actively owned by another user in a workshared/cloud-shared model as a per-element skip with a recorded, non-technical reason, rather than surfacing Revit's native dialog to the user.
- **FR-025**: The system MUST, before attempting an Instance-level write, check whether the target element is a member of a Revit Model Group and, if so, skip it with a recorded, non-technical reason rather than attempting the write, entering Revit's "Edit Group" mode, or automatically ungrouping and regrouping the element.
- **FR-026**: The system MUST present a final summary after the batch runs (fully, partially, or not at all) showing, for the Instance path, the count of elements updated and skipped, or, for the Type path, the affected type(s) and the total count of elements updated across the model.

**Error & Warning Classification**

- **FR-027**: The system MUST classify every abnormal condition or skip reason raised during a session into one of two severities: a warning-level code (400) for conditions that still allow partial batch progress, or an error-level code (500) for conditions that prevent the requested operation from proceeding.
- **FR-028**: The system MUST associate every 400/500 code with a non-technical, end-user-facing message that does not require software or Revit API knowledge to understand.
- **FR-029**: The system MUST display the applicable non-technical message(s) to the user whenever a 400 or 500 condition occurs during the session.
- **FR-030**: The system MUST include, in the final batch summary, the classification code(s) associated with any skipped elements or session-level issues.
- **FR-031**: The system MUST persist the classification code(s) shown in the final summary as part of the session's stored log record.
- **FR-032**: The system MUST treat an empty element selection, an empty/blank replacement value, no parameter chosen from either dialog, an unmodifiable document, and no active document as error-level (500) conditions that block the batch from running.
- **FR-033**: The system MUST treat a missing, read-only, or non-text Instance-level target parameter on an individual element; an element that cannot be edited due to a workshared ownership conflict or any other auto-suppressed native Revit dialog; and an element that is a member of a Revit Model Group; as warning-level (400) per-element conditions that do not block the rest of the batch.

**Traceability & Session Logging**

- **FR-034**: The system MUST treat each add-in invocation, from launch through summary (or cancellation), as one distinct session with its own unique identifier.
- **FR-035**: The system MUST persist a session-scoped, human-readable `.txt` log capturing the session's activity for later review.
- **FR-036**: The system MUST make the persisted session log available on the machine where the session ran, independent of whether Revit or the add-in remains open.
- **FR-037**: The system MUST NOT require any network connection or external service to persist or retrieve session logs.
- **FR-038**: The system MUST name each session's log file and metrics file using a unique identifier composed of the Revit session/run identifier and the active document's name (in the form `{runId}-{documentName}`), so that sessions remain separate and identifiable from one another and from other documents.

**Session Metrics**

- **FR-039**: The system MUST record, per session, the elapsed time spent in the parameter-discovery/search phase and the elapsed time spent in the batch-execution phase, covering the parameter-replacement process end-to-end.
- **FR-040**: The system MUST record, per session, the parameter name(s) and search terms the user looked up via the shared search input, and which dialog(s)/binding each matched result belonged to.
- **FR-041**: The system MUST record, per session, the parameter and replacement value that were ultimately applied (or attempted), and whether the Instance or Type path was used.
- **FR-042**: The system MUST generate, per session, an aggregated count of the operation's outcomes — successes, warnings (400), and errors (500) — grouped both by classification type and by the Revit element category involved.
- **FR-043**: The system MUST persist the session metrics record as NDJSON (newline-delimited JSON, one JSON object per record) so that it can be queried line-by-line without a custom parser, at the managed metrics location, separate from but traceable to the same session log via the shared session identifier (FR-038).

**Packaging & Installer**

- **FR-044**: The system MUST be distributable as an installer that a user can run to make the add-in available in Revit without manually copying project files.
- **FR-045**: The system MUST state, in the accompanying documentation, exactly which Revit version(s) the installer targets.
- **FR-046**: The system MUST NOT claim compatibility with a Revit version that has not been intentionally targeted and verified.
- **FR-047**: The system MUST, once the installer has finished downloading/building, launch an interactive installer UI that lets the user install, uninstall, or update the add-in's version(s) according to the Revit version(s) detected on the system, following researched patterns / reference sources for an interactive installer host.
- **FR-048**: The system MUST support Revit 2025 and 2026 from a single shared codebase/solution. Shared Revit-adapter source lives in one Visual Studio Shared Project; each supported year is a thin SDK-style project that imports that source, references that year's `RevitAPI.dll`/`RevitAPIUI.dll`, and emits a year-suffixed add-in assembly (the ipx.bimops / IP Catalog pattern).

**Ribbon & Application bootstrap**

- **FR-049**: The system MUST, on Revit startup, load an add-in application class (`App` implementing `IExternalApplication`) that registers a custom ribbon panel and a dedicated push-button whose command class is the batch parameter update `IExternalCommand`.
- **FR-050**: The system MUST present that dedicated panel and push-button as the user-facing launch path; the command MUST NOT be available only as a generic dump on the Add-Ins tab.
- **FR-051**: The system MUST use the assignment-provided lineal-color optimization icons as the ribbon button artwork (colors taken from that asset). Those files are copied into the add-in project as embedded or content resources (typically 16px and 32px for the Revit ribbon; if the provided source is only larger PNGs, 16px/32px sizes are derived at implementation). The source location for the assignment assets is `C:\Users\Juan -- IP\Descargas\icons8-optimization-lineal-color`; in-repo copies live under `src/BatchParamUpdate.Adapters.Revit/Resources/`.

### Key Entities

- **Selection Context**: The set of elements in scope for a session, how that scope was established (pre-existing vs. manual pick), and its validity state.
- **Instance Parameter Candidate Set**: The distinct writable, Instance-bound, text-typed parameters present on at least one element in the current selection scope, deduplicated to one entry each regardless of how many elements share them; backs Dialog Box 1.
- **Type Parameter Candidate Set**: The distinct writable, Type-bound, text-typed parameters present on at least one selected element's type, deduplicated to one entry each; backs Dialog Box 2. Selecting and applying one is a model-wide operation affecting every element sharing the matching type, not only the current selection.
- **Shared Search Query**: The single live text filter state that is applied simultaneously against both the Instance Parameter Candidate Set (Dialog Box 1) and the Type Parameter Candidate Set (Dialog Box 2).
- **Replacement Operation**: The chosen target parameter (with its resolved binding — Instance or Type), the new text value, and either the element selection scope (Instance path) or the resolved type(s) (Type path) it will be applied against.
- **Batch Execution Result**: The outcome of running a Replacement Operation — for the Instance path, counts of updated and skipped elements with per-element skip reasons (including workshared-ownership conflicts, Model Group membership, and other auto-suppressed Revit dialogs); for the Type path, the affected type(s) and the total element count updated across the model.
- **Error/Warning Code Catalog**: The set of 400 (warning) and 500 (error) classification codes, each with a stable identifier and a non-technical end-user message.
- **Session Record**: The unique identifier (`{runId}-{documentName}` on disk; `SessionRecord.SessionId` = `revit-{runId}-{documentName}`) tying together one add-in invocation's log, metrics, and batch execution result.
- **Session Log**: The persisted, human-readable `.txt` account of a session's activity.
- **Session Metrics Record**: The persisted NDJSON record of a session's search terms (with the dialog/binding they matched), timings, applied parameter/value/path, and outcome codes aggregated by type and element category.
- **Installer Package**: The distributable artifact that installs the add-in into a Revit environment, along with its stated supported Revit version(s), and that launches an interactive install/uninstall/update UI.
- **Ribbon Panel**: The custom Revit ribbon panel created at add-in startup by `App` (`IExternalApplication`). Host/adapter concern only — not a domain entity.
- **Push Button**: The dedicated ribbon push-button that launches the batch parameter update `IExternalCommand`.
- **Graphic Assets**: The lineal-color optimization PNG resources copied into `src/BatchParamUpdate.Adapters.Revit/Resources/` and wired as the button's small/large images.

## Success Criteria *(mandatory)*

- **SC-001**: A user with elements already selected in Revit can launch the tool and reach the parameter-replacement step without performing any extra selection step.
- **SC-002**: A user with no prior selection can, from inside the tool, pick elements and reach the same parameter-replacement step as a user who preselected.
- **SC-003**: 100% of batch runs, whether fully successful, partially successful, or blocked, end with a summary that states the outcome (elements updated/skipped for the Instance path, or affected type and element count for the Type path).
- **SC-004**: 100% of skipped elements in an Instance-path summary have a stated, non-technical reason that a person without a technical background can understand.
- **SC-005**: If a batch operation cannot proceed at all, the elements in scope retain their original parameter values with no partial modification.
- **SC-006**: A user typing a single search query sees both Dialog Box 1 (Instance parameters) and Dialog Box 2 (Type parameters) update live and simultaneously, without needing to submit, reopen, or repeat the search in a second box.
- **SC-007**: For 100% of completed or attempted sessions, a reviewer can, after the fact, determine what parameter/value was searched or applied, on which path, and what the outcome was — including outcome counts grouped by type and element category — using only files left behind on the machine, named by session and document, with no live Revit session required.
- **SC-008**: A new evaluator can obtain the installer, run it, and use its install/uninstall/update UI to make the add-in available for use inside a supported, detected Revit version, without manually copying files or building source code.
- **SC-009**: The documentation accompanying the deliverable states, without ambiguity, which Revit version(s) are supported, and every claimed version has been exercised at least once.
- **SC-010**: 100% of Type-level parameter selections display the model-wide-effect warning inline, at the moment of selection, without interrupting or requiring extra confirmation before the user can proceed.
- **SC-011**: 100% of workshared/ownership conflicts encountered during a batch run are resolved without the user having to manually dismiss or approve a native Revit dialog.
- **SC-012**: Users can observe a running batch's progress within the same window used for the rest of the flow, with no additional popup appearing.
- **SC-013**: 100% of Instance-level updates targeting an element that is a member of a Revit Model Group are skipped without attempting the write, without entering Revit's interactive Edit Group mode, and without ungrouping the element, while the rest of the batch continues uninterrupted.
- **SC-014**: After install, a user can start the tool from a dedicated custom ribbon panel push-button (with the provided optimization artwork), without relying on a generic Add-Ins tab command dump.

## Assumptions & Mandated Constraints

The following are treated as decisions already made by the stakeholder who commissioned this expanded scope, not as open questions:

- **Technology stack**: WPF for all user-facing dialogs; C#/.NET; Autodesk Revit API, targeting Revit **2025 and 2026**; built with Visual Studio.
- **Architecture**: A Domain/Hexagonal-hybrid architecture using ports-and-adapters is mandated specifically to decouple business logic from the Revit API, so that the batch-update logic, error classification, and reporting can be exercised and tested without a running Revit host.
- **Logging ownership**: Logging capability lives in the solution's "core" layer and is inherited/extended by the domain and application layers, rather than being implemented independently per layer.
- **Session storage locations**: Per-session metrics are written under `%LOCALAPPDATA%\juanManriqueHexagon\TRACKER`; per-session logs are written in a parallel folder at `%LOCALAPPDATA%\juanManriqueHexagon\LOGS`. These are fixed locations that do not follow `TMP`/`TEMP` (which some hosts redirect to `C:\Temp\{guid}\`). Not user-configurable in this submission.
- **Session/metrics file naming**: Both the `.txt` session log and the JSON Lines metrics file use the same unique name, `{runId}-{documentName}`, so a session's two artifacts can always be paired and located by name alone.
- **Metrics format**: Session metrics are written as NDJSON (one JSON object per line) specifically so they can be queried with simple line-oriented tools without a bespoke parser, and are aggregated by outcome type (success/warning/error) and by Revit element category.
- **Error/Warning taxonomy**: Every abnormal condition is classified as either 400 (warning — partial progress still possible) or 500 (error — operation blocked), each carrying a non-technical message for the end user; the code is persisted alongside the final summary in the session log.
- **Four delivery pillars**: The submission is explicitly organized around Functionality, Traceability, Observability, and Testability as evaluation dimensions, which is why logging, metrics, and a decoupled architecture are in scope even though the base assessment brief did not require them.
- **Installer engine**: Velopack is the mandated packaging/installer engine for this deliverable; once packaging finishes, the resulting installer launches its own interactive UI for installing, uninstalling, or updating the add-in per detected Revit version(s), following researched patterns / reference sources for an interactive installer host.
- **Ribbon bootstrap**: Revit loads `App` (`IExternalApplication`) from the `.addin` manifest `Application` class. `OnStartup` creates a custom `RibbonPanel` and a dedicated `PushButton` that invokes the batch `IExternalCommand`. Graphic resources are the provided lineal-color optimization icons copied into `src/BatchParamUpdate.Adapters.Revit/Resources/` (source: `C:\Users\Juan -- IP\Descargas\icons8-optimization-lineal-color`; provided files are 64px and 100px PNGs — 16px/32px ribbon sizes are derived at implementation if needed).
- **Multi-version project setup**: Revit 2025 and 2026 support is delivered from one shared solution: adapter source lives in `BatchParamUpdate.Adapters.Revit` (a Visual Studio Shared Project); each year is a thin project (`BatchParamUpdate.Adapters.Revit.2025` / `.2026`) that imports that source, references that year's Revit API, and uses Debug/Release like any other project. Domain, Application, Core, UI, and Installer stay year-neutral. This matches the ipx.bimops (`ipx.bimops.revit.20XX` + `.projitems`) and IP Catalog (`IPX.Catalog.Revit.20XX`) pattern.
- **Parameter discovery is a deduplicated union, not an intersection**: A parameter is listed once in the relevant dialog's list as soon as it exists — with the correct binding and text type — on **at least one** element in the current selection scope; it is never required to exist on every selected element. Elements lacking the chosen parameter at execution time are skipped individually and reported; this is the expected, common execution outcome, not a rare edge case.
- **Dialog Box 1 and Dialog Box 2 are simultaneous, jointly-searched, and binding-based, not category- or scope-based**: Both dialogs are displayed together at all times over the identical current element selection scope and share a single search input. Dialog Box 1 lists writable text parameters bound at the **Instance** level; Dialog Box 2 lists writable text parameters bound at the **Type** level. Both are actionable — searchable and selectable as an update target. Which dialog a given parameter appears in is decided purely by its Revit binding, resolved automatically by the system.
- **Type-level updates have a wider blast radius than the current selection, by Revit's design**: A Type parameter's value is shared by every element instantiated from that type. Applying a change via Dialog Box 2 therefore updates every element in the model using the affected type — not only the elements the user selected. This is not treated as an error and is not gated behind a blocking confirmation dialog; it is disclosed to the user inline, in the UI, at the moment the Type parameter is selected (FR-014), and is reflected explicitly in the session summary and log so it is never a silent side effect.
- **Native Revit dialog suppression during batch execution**: Interactive Revit dialogs that would otherwise require per-element dismissal during a batch run — most notably workshared/cloud-model requests to edit an element owned by another user — are intercepted and suppressed automatically so the user is never asked to approve or deny them one instance at a time; the outcome for the affected element is instead recorded as a standard per-element skip.
- **Model Group membership blocks Instance-level edits, by long-standing Revit API design, not by choice**: The Revit API exposes no programmatic "Edit Group" scope. Calling a parameter setter on an element that is a member of a Model Group from outside interactive Edit Group mode either throws a hard failure (when that group's type has more than one instance in the model) or only succeeds by suppressing an unstable warning for a lone instance — a workaround that the Revit developer community (The Building Coder, Autodesk's own Developer Blog, and multiple Autodesk/Dynamo forum threads) explicitly warns can corrupt or crash large models, and which remains unaddressed as of Revit 2026 per community reports at the time of writing. This submission does not attempt that workaround, nor the alternative "ungroup, edit, regroup" pattern also documented by Autodesk; a Model Group member targeted for an Instance-level update is proactively identified and skipped (400) instead. A Type-level update (Dialog Box 2) targets the shared Type/Symbol object directly rather than the grouped instance, so it is expected to remain unaffected by this restriction — this expectation should be re-verified against the live Revit 2025-2026 API during implementation, since it was not the primary focus of the research behind this assumption.
- **Progress reporting is inline, not a popup**: Batch-execution progress is shown within the same window as the rest of the flow, never as a separate progress dialog.
- **Session boundary**: One "session" is one add-in invocation, from launch through either a displayed summary or an explicit cancellation; metrics and logs are scoped to that single invocation.
- **Supported Revit versions are a closed, stated list**: Only Revit 2025 and 2026 are claimed as supported; no other version is implied or tested.

## Future Enhancements (Deferred to V2 — Out of Current Scope)

- **Informational, non-text parameter browser**: An earlier draft of this spec included a third, purely informational dialog that surfaced parameters common to the current selection scope that are of any non-text data type (boolean, integer, double/number, etc.), toggle-able by parameter type, intended as a diagnostic aid — so a user who cannot find an expected parameter in Dialog Box 1 or Dialog Box 2 could confirm whether it exists under a different data type without inspecting the Revit model directly. This improves discoverability and reduces support burden but is not required for the core batch-update deliverable and is explicitly deferred out of the current submission's scope. It may be revisited in a future version alongside the current Dialog Box 1 / Dialog Box 2 experience.

## Out of Scope

- Any parameter type other than writable, text/string-typed parameters — whether Instance-bound (Dialog Box 1) or Type-bound (Dialog Box 2); no numeric, length, yes/no, or other non-text data types as update targets on either path.
- Operating on more than one open Revit document at a time, or on links/linked-model elements.
- Any cloud-hosted telemetry, remote logging service, or Application Performance Monitoring integration — observability in this submission is file-based and local to the machine, by stakeholder mandate.
- User authentication, authorization, or multi-user access control of any kind (beyond skipping elements owned by another user, which is a read of existing Revit worksharing state, not an access-control feature this add-in implements).
- Localization/internationalization of the UI or messages beyond English.
- A purely informational, non-text parameter browser (see "Future Enhancements (Deferred to V2)" above).
- Creating new parameters, or modifying parameter definitions, bindings, or shared parameter files.
- A model-wide "find and replace" that operates independently of an explicit element selection (the Type-path's model-wide effect is a consequence of Revit's Type-parameter binding, not an independent find-and-replace feature).
- Automatically ungrouping, editing, and regrouping Revit Model Groups (or entering Revit's interactive "Edit Group" mode programmatically) to force an Instance-level parameter update on a grouped element — a known, unstable Revit API limitation; affected elements are skipped and reported instead of having their groups restructured.
- Undo/redo behavior beyond what Revit's native transaction/undo stack already provides.
- Non-Windows platforms or non-Revit hosts.

The original technical assessment brief estimated 4-6 hours of effort for the base flow (selection, single dialog, batch update, summary). The scope actually implemented in this submission — hexagonal architecture with ports and adapters, two simultaneous, jointly-searched parameter discovery/search/replace paths (Instance and Type), automatic suppression of native Revit dialogs during execution, a formal 400/500 classification catalog, per-session NDJSON metrics and logs persisted to disk, and a Velopack-based installer with its own install/uninstall/update UI — is materially larger than that estimate because it was explicitly expanded by the stakeholder to demonstrate the four delivery pillars (Functionality, Traceability, Observability, Testability) rather than the minimal base flow alone. This is documented here so evaluators reviewing effort-to-scope ratio understand the estimate applies to the base brief, not to the mandated expanded submission.

## Dependencies

- A licensed, installed copy of Autodesk Revit — version 2025 or 2026 — on the evaluator's machine.
- A Windows OS environment with a writable system Temp directory (required for session logs and metrics persistence).
- .NET runtime/SDK compatible with the targeted Revit API versions, for building and running the add-in (per-year adapter projects: `net8.0-windows` for 2025/2026).
- Visual Studio, for building the solution from source.
- Velopack tooling (`vpk`) plus the Velopack NuGet package on the installer host, which must call `VelopackApp.Build().Run()` in `Program.Main` before any WPF window, for producing the installer package and its install/uninstall/update UI from the built solution.
