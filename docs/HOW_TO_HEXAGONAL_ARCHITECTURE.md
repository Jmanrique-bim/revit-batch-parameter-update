# HOW_TO: hexagonal architecture

Domain and Application never reference `RevitAPI.dll`, UI, or persistence. Everything outward talks to them through ports. There is no DI container: `CompositionRoot` (`src/BatchParamUpdate.Adapters.Revit/Composition/CompositionRoot.cs`) is the single place that constructs adapters and injects them. `LayerDependencyTests` fails the build if Domain/Application ever gain an outward reference.

## Layers

| Project | Role |
|---|---|
| `BatchParamUpdate.Domain` | Entities, ports, `ParameterWriteDecision`, error/warning catalog. No I/O. |
| `BatchParamUpdate.Application` | Use cases, `BatchUpdateCoordinator`, `WorkflowEvent` / `SessionTraceListener`. May use `Core` timers. |
| `BatchParamUpdate.Core` | `SessionFileLogger`, `SessionStoragePaths`, `RunIdGenerator`, `DocumentNameSanitizer`, `PhaseTimer`. |
| `BatchParamUpdate.Adapters.Revit` | Shared source: `App`, command, `CompositionRoot`, the Revit port adapters. |
| `BatchParamUpdate.Adapters.Revit.2025 / .2026` | Thin shells: TFM, RevitAPI HintPath, `.addin`, import `Year.props`. |
| `BatchParamUpdate.Adapters.Persistence` | `NdjsonSessionRecorder`, `CsvSkipReportExporter`. |
| `BatchParamUpdate.UI.Wpf` | Views + view-models; call the coordinator / Application, not RevitAPI. |
| `BatchParamUpdate.Installer` | Standalone WPF; `IInstallerPort` / `RevitInstallerAdapter`. |
| `tests/BatchParamUpdate.Tests.Unit` | xUnit + hand-written fakes. |

Year shells are the only projects that reference RevitAPI. Ribbon / `App` live in the shared adapter source; they are not a port.

## Ports

Under `src/BatchParamUpdate.Domain/Ports/`:

| Port | Production adapter |
|---|---|
| `IElementSelectionPort` | `Adapters.Revit.Selection.RevitElementSelectionPort` |
| `IParameterDiscoveryPort` | `Adapters.Revit.Discovery.RevitParameterDiscoveryPort` |
| `IParameterWritePort` | `Adapters.Revit.Writing.RevitParameterWritePort` |
| `INativeDialogSuppressionPort` | `Adapters.Revit.DialogSuppression.RevitDialogSuppressionPort` |
| `IWorksharingStatusPort` | `Adapters.Revit.Worksharing.RevitWorksharingStatusPort` |
| `ILoggerPort` | `Core.SessionFileLogger` |
| `ISessionRecorderPort` | `Adapters.Persistence.NdjsonSessionRecorder` |
| `IReportExportPort` | `Adapters.Persistence.CsvSkipReportExporter` |
| `IInstallerPort` | `Installer.RevitInstallerAdapter` (installer process, not the add-in) |

Tracing is not a port: the coordinator raises `WorkflowEvent`s to an `IWorkflowObserver` (Application-internal); `SessionTraceListener` is the production observer.

## Dependency rule

Inward only: adapters → Application → Domain. Tests substitute fakes for adapters. If a type needs `Document` / `UIDocument`, it belongs in `Adapters.Revit`. `CompositionRoot` is the one file allowed to see UI + persistence + Revit together.
