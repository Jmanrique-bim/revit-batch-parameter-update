using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BatchParamUpdate.Adapters.Persistence;
using BatchParamUpdate.Adapters.Revit.DialogSuppression;
using BatchParamUpdate.Adapters.Revit.Discovery;
using BatchParamUpdate.Adapters.Revit.ExternalEvents;
using BatchParamUpdate.Adapters.Revit.Selection;
using BatchParamUpdate.Adapters.Revit.Worksharing;
using BatchParamUpdate.Adapters.Revit.Writing;
using BatchParamUpdate.Application.Observability;
using BatchParamUpdate.Application.UseCases;
using BatchParamUpdate.Application.Workflow;
using BatchParamUpdate.Core;
using BatchParamUpdate.Domain.Model;
using BatchParamUpdate.Domain.Ports;
using BatchParamUpdate.UI.Wpf.ViewModels;

namespace BatchParamUpdate.Adapters.Revit.Composition;

/// <summary>
/// The one place that is allowed to see UI, persistence and Revit together. It wires the ports,
/// use cases, coordinator and view-models; the external command only drives the result.
/// </summary>
public sealed record CompositionResult(
    MainViewModel View,
    BatchUpdateCoordinator Coordinator,
    RevitApiEventBridge RevitBridge);

public static class CompositionRoot
{
    public static CompositionResult Build(
        UIApplication uiapp,
        Document doc,
        string runId,
        string documentName,
        ILoggerPort logger,
        Action? hideHost,
        Action? showHost)
    {
        var identity = new SessionRecord(runId, documentName, DateTimeOffset.UtcNow);
        var observer = new SessionTraceListener(
            new NdjsonSessionRecorder(runId, documentName, logger), logger, identity);

        var selectionPort = new RevitElementSelectionPort(uiapp.ActiveUIDocument);
        var discoveryPort = new RevitParameterDiscoveryPort(doc);
        var dialogs = new RevitDialogSuppressionPort(uiapp);
        var worksharing = new RevitWorksharingStatusPort(doc);
        var writePort = new RevitParameterWritePort(doc, dialogs, worksharing);
        var revitBridge = new RevitApiEventBridge();

        var session = new Session();
        var state = new WorkflowState();
        var coordinator = new BatchUpdateCoordinator(
            session,
            state,
            new EstablishSelectionUseCase(selectionPort),
            new DiscoverParametersUseCase(discoveryPort),
            new RunBatchUpdateUseCase(writePort),
            observer,
            identity.SessionId);

        var selectionResult = coordinator.EstablishSelection();
        var manualPickAllowed = selectionResult is SelectionResult.NeedsManualPick;

        var search = new SharedSearchViewModel(new ParameterSearch(coordinator.Candidates));
        var execution = new BatchExecutionViewModel();
        var summary = new BatchSummaryViewModel(new CsvSkipReportExporter(), runId);
        var select = new SelectElementsViewModel(selectionPort, coordinator, manualPickAllowed, hideHost, showHost);
        var discovery = new ParameterDiscoveryViewModel(coordinator, search);

        // Modeless: the window outlives Execute, so the write can fire after `doc` was closed.
        // `IsValidObject` is false once Revit has disposed the document — refuse rather than let
        // `new Transaction(doc, ...)` throw. (Don't compare against ActiveUIDocument.Document:
        // Revit hands back a fresh managed wrapper each call, so reference identity is unreliable.)
        Task RunOnRevit(Action work) => revitBridge.RunAsync(() =>
        {
            if (!doc.IsValidObject)
                throw new InvalidOperationException("The target document is no longer open in Revit.");
            work();
        });

        var replacement = new ReplacementValueViewModel(coordinator, execution, summary, RunOnRevit);

        var view = new MainViewModel(coordinator, select, search, discovery, replacement, execution, summary);
        return new CompositionResult(view, coordinator, revitBridge);
    }
}
