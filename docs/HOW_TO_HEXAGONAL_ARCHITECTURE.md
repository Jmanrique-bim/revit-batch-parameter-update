# HOW_TO: hexagonal architecture

Domain and Application never reference `RevitAPI.dll`. The Revit process, filesystem, and installer talk to them only through ports. There is no DI container: `BatchParameterUpdateCommand` constructs adapters and injects them.

## Layers

| Project | Role |
|---|---|
| `BatchParamUpdate.Domain` | Entities, 8 ports, error/warning catalog. No Core, no I/O. |
| `BatchParamUpdate.Application` | Use cases. May use `Core` timers. Depends on Domain ports. |
| `BatchParamUpdate.Core` | `SessionFileLogger`, `SessionStoragePaths`, `RunIdGenerator`, `DocumentNameSanitizer`, `PhaseTimer`. |
| `BatchParamUpdate.Adapters.Revit` | Shared source: `App`, command, four Revit port adapters. |
| `BatchParamUpdate.Adapters.Revit.20XX` | Thin shells: TFM, RevitAPI HintPath, `.addin`, import `Year.props`. |
| `BatchParamUpdate.Adapters.Persistence` | `NdjsonSessionRecorder`, `CsvSkipReportExporter`. |
| `BatchParamUpdate.UI.Wpf` | Views + ViewModels; calls use cases or Domain ports, not RevitAPI. |
| `BatchParamUpdate.Installer` | Standalone WPF; `IInstallerPort` / `RevitInstallerAdapter`. |
| `tests/BatchParamUpdate.Tests.Unit` | xUnit + hand-written fakes of the original 7 ports. |

Year shells are the only projects that reference RevitAPI. Ribbon/`App` live in the shared adapter source; they are **not** a port.

## Eight ports

Contracts: `docs/specs/001-batch-parameter-update/contracts/ports.md` (the original 7; `IReportExportPort` is a follow-up, documented there as an addendum). Interfaces under `src/BatchParamUpdate.Domain/Ports/`.

| Port | Production adapter |
|---|---|
| `IElementSelectionPort` | `Adapters.Revit.Selection.RevitElementSelectionPort` |
| `IParameterDiscoveryPort` | `Adapters.Revit.Discovery.RevitParameterDiscoveryPort` |
| `IParameterWritePort` | `Adapters.Revit.Writing.RevitParameterWritePort` |
| `INativeDialogSuppressionPort` | `Adapters.Revit.DialogSuppression.RevitDialogSuppressionPort` |
| `ILoggerPort` | `Core.SessionFileLogger` |
| `ISessionRecorderPort` | `Adapters.Persistence.NdjsonSessionRecorder` |
| `IInstallerPort` | `Installer.RevitInstallerAdapter` (installer process, not the add-in command) |
| `IReportExportPort` | `Adapters.Persistence.CsvSkipReportExporter` — writes the batch summary's skip list to CSV under `%USERPROFILE%\Downloads\`. Injected into `BatchSummaryViewModel` (same pattern as `IElementSelectionPort` on `SelectElementsViewModel`); no Application use case, because the only empty-file guard is `ExportCommand.CanExecute` (`HasSkips`). |

## Dependency rule

Inward: adapters → Application → Domain. Tests substitute fakes for adapters. If a type needs `Document` or `UIDocument`, it belongs in `Adapters.Revit`.

## Diagrams

- Layers: `docs/diagrams/hexagonal-layers.html` (source: `hexagonal-layers.architecture.json`)
- Ports at runtime: `docs/diagrams/hexagonal-ports.html` (source: `hexagonal-ports.sequence.json`)
