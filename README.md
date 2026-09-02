# Batch Parameter Update

Revit add-in that batch-writes one writable **text** parameter across a selection. Instance-bound and Type-bound candidates are discovered from the same scope, shown together, and filtered by a single search. The chosen binding decides the write path.

**Author:** Juan Pablo Manrique

**Who it is for:** BIM/Revit users who need a reversible, logged batch text-parameter update, and engineers extending this Hexagon assessment add-in.

Supported hosts: **Autodesk Revit 2025, 2026, and 2027** only. The installer never offers Install/Update/Uninstall for any other year.

## How it works

Hexagonal (ports and adapters) inside the Revit process, plus MVVM for the WPF window.

1. `App` (`IExternalApplication`) registers a ribbon panel **Batch Parameter Update** and a **Batch Update** button.
2. `BatchParameterUpdateCommand` is the composition root: it wires adapters, use cases, session recording, and the WPF window, then `ShowDialog()`.
3. Application use cases talk only to Domain ports. Year shells (`Adapters.Revit.20XX`) compile the shared Revit adapter source against that year's `RevitAPI.dll`.
4. Unit tests in `tests/BatchParamUpdate.Tests.Unit` exercise Domain/Application with in-memory fakes — no RevitAPI.

See [docs/HOW_TO_HEXAGONAL_ARCHITECTURE.md](docs/HOW_TO_HEXAGONAL_ARCHITECTURE.md) and [docs/HOW_TO_RUN.md](docs/HOW_TO_RUN.md).

## Repository layout

```
BatchParamUpdate.sln
src/
  BatchParamUpdate.Domain/                 # model, 7 ports, error catalog (no RevitAPI)
  BatchParamUpdate.Application/            # use cases
  BatchParamUpdate.Core/                   # SessionFileLogger, runId, timers (no RevitAPI)
  BatchParamUpdate.Adapters.Revit/         # shared adapter source (App, command, ports)
  BatchParamUpdate.Adapters.Revit.2025/    # thin year shell + .addin (net8.0-windows)
  BatchParamUpdate.Adapters.Revit.2026/    # same for 2026
  BatchParamUpdate.Adapters.Revit.2027/    # same for 2027 (net10.0-windows)
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

```powershell
.\pack.ps1 -Version 1.0.0
```

That publishes `Installer.exe`, copies each year payload, and runs `vpk pack -u BatchParamUpdate -e Installer.exe`. The installer UI lists detected 2025/2026/2027 installs and copies the matching assembly plus `.addin` (`Application` class = `App`).

Session artifacts:

- Logs: `%LOCALAPPDATA%\juanManriqueHexagon\LOGS\{runId}-{documentName}.txt`
- Metrics: `%LOCALAPPDATA%\juanManriqueHexagon\TRACKER\{runId}-{documentName}.json`

## Documentation

- [docs/README.md](docs/README.md) — HOW_TO index and diagrams
- Spec kit (requirements, ports, data model): [docs/specs/001-batch-parameter-update/](docs/specs/001-batch-parameter-update/)
- Host validation scenarios: [docs/specs/001-batch-parameter-update/quickstart.md](docs/specs/001-batch-parameter-update/quickstart.md)
