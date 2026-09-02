using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BatchParamUpdate.Adapters.Persistence;
using BatchParamUpdate.Adapters.Revit.DialogSuppression;
using BatchParamUpdate.Adapters.Revit.Discovery;
using BatchParamUpdate.Adapters.Revit.Selection;
using BatchParamUpdate.Adapters.Revit.Writing;
using BatchParamUpdate.Application.UseCases;
using BatchParamUpdate.Core;
using BatchParamUpdate.Domain.ErrorCatalog;
using BatchParamUpdate.Domain.Model;
using BatchParamUpdate.UI.Wpf.ViewModels;
using BatchParamUpdate.UI.Wpf.Views;

namespace BatchParamUpdate.Adapters.Revit.ExternalCommand;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public sealed class BatchParameterUpdateCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var uiapp = commandData.Application;
        var uidoc = uiapp.ActiveUIDocument;
        if (uidoc is null)
        {
            message = ErrorWarningCatalog.Message(ErrorCode.NoActiveDocument);
            TaskDialog.Show("Batch Parameter Update", message);
            return Result.Failed;
        }

        var doc = uidoc.Document;
        if (doc.IsReadOnly)
        {
            message = ErrorWarningCatalog.Message(ErrorCode.DocumentNotModifiable);
            TaskDialog.Show("Batch Parameter Update", message);
            return Result.Failed;
        }

        var runId = RunIdGenerator.NewRunId();
        var documentName = DocumentNameSanitizer.Sanitize(doc.Title);
        using var logger = new SessionFileLogger(runId, documentName);
        var metrics = new NdjsonSessionRecorder(runId, documentName, logger);
        var record = new RecordSessionUseCase(
            metrics,
            logger,
            new SessionRecord(runId, documentName, DateTimeOffset.UtcNow));
        record.Start();
        record.Trace($"Command start document={doc.Title} runId={runId}");

        var session = new Session();
        try
        {
            var selectionPort = new RevitElementSelectionPort(uidoc);
            var establish = new EstablishSelectionUseCase(selectionPort);
            var preExisting = selectionPort.GetPreExistingSelection();
            var scope = preExisting.IsValid
                ? establish.Execute(session)
                : new SelectionContext([], SelectionOrigin.ManualPick);
            record.Trace($"Selection origin={scope.Origin} count={scope.ElementRefs.Count}");

            var discoveryPort = new RevitParameterDiscoveryPort(doc);
            var discover = new DiscoverParametersUseCase(discoveryPort, record);
            var (instanceSet, typeSet) = scope.IsValid
                ? discover.Discover(scope)
                : (new InstanceParameterCandidateSet([]), new TypeParameterCandidateSet([]));

            if (scope.IsValid && session.State == SessionState.Started)
                session.TransitionTo(SessionState.Discovering);

            var searchVm = new SharedSearchViewModel(new SharedSearchQuery(instanceSet, typeSet));
            var discoveryVm = new ParameterDiscoveryViewModel(discover, scope, session, searchVm);
            var dialogs = new RevitDialogSuppressionPort(uiapp, doc);
            var run = new RunBatchUpdateUseCase(new RevitParameterWritePort(doc, dialogs), logger, record);
            var executionVm = new BatchExecutionViewModel();
            var summaryVm = new BatchSummaryViewModel(logger.FilePath, metrics.FilePath);

            MainWindow? window = null;
            var selectVm = new SelectElementsViewModel(
                scope,
                selectionPort,
                session,
                beforePick: () => window?.Hide(),
                afterPick: () => window?.Show());

            var replacementVm = new ReplacementValueViewModel(
                () => discoveryVm.Operation,
                () => selectVm.Selection,
                session,
                run,
                executionVm,
                summaryVm);

            selectVm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName != nameof(SelectElementsViewModel.Selection))
                    return;
                var next = selectVm.Selection;
                if (!next.IsValid)
                    return;
                var (instance, type) = discover.Discover(next);
                searchVm.ReplaceSets(instance, type);
                discoveryVm.Retarget(next);
                record.Trace($"Selection updated origin={next.Origin} count={next.ElementRefs.Count}");
            };

            searchVm.TextChanged += (_, _) =>
                record.RecordSearch(
                    searchVm.Text,
                    [.. searchVm.Query.MatchesInstanceSet.Select(c => c.Name)],
                    [.. searchVm.Query.MatchesTypeSet.Select(c => c.Name)]);

            discoveryVm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(ParameterDiscoveryViewModel.Operation))
                    System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            };

            window = new MainWindow();
            window.Bind(selectVm, searchVm, discoveryVm, replacementVm, executionVm, summaryVm);
            window.ShowDialog();
            return Result.Succeeded;
        }
        finally
        {
            if (session.State is not SessionState.Completed and not SessionState.Blocked and not SessionState.Cancelled)
                session.TransitionTo(SessionState.Cancelled);
            record.End(session);
        }
    }
}
