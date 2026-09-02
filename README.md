# Batch Parameter Update

Revit add-in that batch-writes one writable **text instance** parameter across a selection. Candidates are discovered from the current selection, listed in one searchable panel, and the chosen parameter is written on every selected element inside one reversible transaction.

**Author:** Juan Pablo Manrique

**Who it is for:** BIM/Revit users who need a reversible, logged batch text-parameter update, and engineers extending this Hexagon assessment add-in.

Supported hosts: **Autodesk Revit 2025 and 2026** only. The installer never offers Install/Update/Uninstall for any other year.

## How it works

Hexagonal (ports and adapters) inside the Revit process, plus MVVM for the WPF window.

1. `App` (`IExternalApplication`) registers a ribbon panel **Batch Parameter Update** and a **Batch Update** button.
2. `BatchParameterUpdateCommand` is thin: it opens the session log, calls `CompositionRoot.Build(...)`, shows the window, and calls `BatchUpdateCoordinator.Complete()` on close.
3. `CompositionRoot` (the one place that sees UI + persistence + Revit together) wires the ports, use cases, the `BatchUpdateCoordinator`, and the view-models.
4. `BatchUpdateCoordinator` (Application) owns the flow: it is the only component that advances the `Session` and the only source of `WorkflowEvent`s. A single `SessionTraceListener` turns those events into the `.txt` log and NDJSON metrics — the flow logic itself contains no logging.
5. Application use cases talk only to Domain ports. Year shells (`Adapters.Revit.20XX`) compile the shared Revit adapter source against that year's `RevitAPI.dll`.
6. Unit tests in `tests/BatchParamUpdate.Tests.Unit` exercise Domain/Application with in-memory fakes — no RevitAPI. A `LayerDependencyTests` check fails if Domain/Application ever gain an outward dependency.

See [docs/HOW_TO_HEXAGONAL_ARCHITECTURE.md](docs/HOW_TO_HEXAGONAL_ARCHITECTURE.md) and [docs/HOW_TO_RUN.md](docs/HOW_TO_RUN.md).

## Repository layout

```
BatchParamUpdate.sln
src/
  BatchParamUpdate.Domain/                 # model, ports, decision logic, error catalog (no RevitAPI)
  BatchParamUpdate.Application/            # use cases, coordinator, observability
  BatchParamUpdate.Core/                   # SessionFileLogger, runId, timers (no RevitAPI)
  BatchParamUpdate.Adapters.Revit/         # shared adapter source (App, command, ports)
  BatchParamUpdate.Adapters.Revit.2025/    # thin year shell + .addin (net8.0-windows)
  BatchParamUpdate.Adapters.Revit.2026/    # same for 2026
  BatchParamUpdate.Adapters.Persistence/   # NDJSON metrics
  BatchParamUpdate.UI.Wpf/                 # MainWindow + ViewModels
  BatchParamUpdate.Installer/              # Velopack WPF installer
tests/BatchParamUpdate.Tests.Unit/
docs/                                      # HOW_TO guides, diagrams, spec kit
```

## Build, debug, install

Year adapters need that year's `RevitAPI.dll` / `RevitAPIUI.dll` under `%ProgramW6432%\Autodesk\Revit {year}`.

```powershell
dotnet test tests/BatchParamUpdate.Tests.Unit/BatchParamUpdate.Tests.Unit.csproj
dotnet build BatchParamUpdate.sln -c Release
```

A **Debug** build of a year project copies the `.addin` and payload to `%AppData%\Autodesk\REVIT\Addins\{year}` (see `src/BatchParamUpdate.Adapters.Revit/Year.props`). Launch Revit for that year and use the ribbon button. The command requires an active, modifiable document.

Installer (from `src/BatchParamUpdate.Installer/`):

Prerequisites: Velopack CLI (`vpk`) on PATH. `Installer.exe` must call `VelopackApp.Build().Run()` before any WPF window (`Program.Main` + Velopack 1.2.0) or `vpk pack` refuses the binary. `pack.ps1` fails if `dotnet` / `vpk` return non-zero.

```powershell
.\pack.ps1 -Version 1.0.0
```

That publishes `Installer.exe`, copies each year payload, and runs `vpk pack -u BatchParamUpdate -e Installer.exe`. The installer UI lists detected 2025/2026 installs and copies the matching assembly plus `.addin` (`Application` class = `App`) into the **per-user** add-ins folder `%APPDATA%\Autodesk\Revit\Addins\{year}` — no administrator rights required. Install and uninstall also best-effort remove any manifest an older version left under `%ProgramData%\Autodesk\Revit\Addins\{year}`.

Session artifacts:

- Logs: `%LOCALAPPDATA%\juanManriqueHexagon\LOGS\{runId}-{documentName}.txt`
- Metrics: `%LOCALAPPDATA%\juanManriqueHexagon\TRACKER\{runId}-{documentName}.json`

## Documentation

- [docs/README.md](docs/README.md) — HOW_TO index and diagrams
- [docs/TESTING.md](docs/TESTING.md) — automated coverage and the manual Revit test matrix
- Spec kit: [docs/specs/001-batch-parameter-update/](docs/specs/001-batch-parameter-update/) (`spec.md`, `plan.md`, `tasks.md`, `checklists/`)
