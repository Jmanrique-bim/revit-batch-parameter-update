# Documentation

Runtime guides for this add-in. Spec-kit requirements live under [specs/001-batch-parameter-update/](specs/001-batch-parameter-update/).

## HOW_TO

- [HOW_TO_RUN.md](HOW_TO_RUN.md) — ribbon → command → UI → write
- [HOW_TO_SELECTION.md](HOW_TO_SELECTION.md) — `SelectionContext`, pre-existing vs pick
- [HOW_TO_DISCOVER_PARAMETERS.md](HOW_TO_DISCOVER_PARAMETERS.md) — Instance + Type candidates, shared search
- [HOW_TO_BATCH_UPDATE.md](HOW_TO_BATCH_UPDATE.md) — transaction, skips, dialog suppression
- [HOW_TO_SESSIONS.md](HOW_TO_SESSIONS.md) — `SessionState`, `.txt` / NDJSON
- [HOW_TO_HEXAGONAL_ARCHITECTURE.md](HOW_TO_HEXAGONAL_ARCHITECTURE.md) — ports, adapters, year shells
- [HOW_TO_MVVM.md](HOW_TO_MVVM.md) — WPF bind map and commands

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
- [contracts/ports.md](specs/001-batch-parameter-update/contracts/ports.md)
- [data-model.md](specs/001-batch-parameter-update/data-model.md)
- [quickstart.md](specs/001-batch-parameter-update/quickstart.md) — host validation scenarios
