# Documentation

Runtime guides for this add-in. Spec-kit requirements live under [specs/001-batch-parameter-update/](specs/001-batch-parameter-update/).

## HOW_TO

- [HOW_TO_RUN.md](HOW_TO_RUN.md) — ribbon → command → composition root → coordinator → write
- [HOW_TO_SELECTION.md](HOW_TO_SELECTION.md) — `SelectionContext`, pre-existing vs pick
- [HOW_TO_DISCOVER_PARAMETERS.md](HOW_TO_DISCOVER_PARAMETERS.md) — instance text-parameter candidates, search
- [HOW_TO_BATCH_UPDATE.md](HOW_TO_BATCH_UPDATE.md) — transaction, `ParameterWriteDecision`, skips
- [HOW_TO_SESSIONS.md](HOW_TO_SESSIONS.md) — `WorkflowEvent`, `SessionTraceListener`, `.txt` / NDJSON
- [HOW_TO_HEXAGONAL_ARCHITECTURE.md](HOW_TO_HEXAGONAL_ARCHITECTURE.md) — ports, adapters, year shells
- [HOW_TO_MVVM.md](HOW_TO_MVVM.md) — WPF bind map and commands
- [TESTING.md](TESTING.md) — automated coverage and the manual Revit test matrix

## Diagrams

Generated Archify HTML under `docs/diagrams/` (gitignored). Open locally:

| Diagram | HTML | Source JSON |
|---|---|---|
| Hexagonal layers | `hexagonal-layers.html` | `hexagonal-layers.architecture.json` |
| Ports at runtime | `hexagonal-ports.html` | `hexagonal-ports.sequence.json` |
| Session flow | `session-flow.html` | `session-flow.workflow.json` |
| Session states | `session-states.html` | `session-states.lifecycle.json` |
| Batch write | `batch-write.html` | `batch-write.sequence.json` |
| MVVM flow | `mvvm-flow.html` | `mvvm-flow.workflow.json` |

## Spec kit

- [spec.md](specs/001-batch-parameter-update/spec.md)
- [plan.md](specs/001-batch-parameter-update/plan.md)
- [tasks.md](specs/001-batch-parameter-update/tasks.md)
- [checklists/requirements.md](specs/001-batch-parameter-update/checklists/requirements.md)
