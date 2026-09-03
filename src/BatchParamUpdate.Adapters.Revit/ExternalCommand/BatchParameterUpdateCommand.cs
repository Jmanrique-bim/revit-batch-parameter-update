using System.Windows.Threading;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BatchParamUpdate.Adapters.Revit.Composition;
using BatchParamUpdate.Core;
using BatchParamUpdate.Domain.ErrorCatalog;
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

        MainWindow? window = null;
        var composition = CompositionRoot.Build(
            uiapp,
            doc,
            runId,
            documentName,
            logger,
            hideHost: () => window?.Hide(),
            showHost: () => window?.Show());

        try
        {
            window = new MainWindow();
            window.Bind(composition.View);
            // ShowDialog + Hide() for PickObjects ends the modal loop on Revit 2026
            // (Finish/Cancel palette). ShowDialog returns, finally Complete() cancels the
            // session ~ms after AdoptManualSelection — log why=no-parameter with no
            // Parameter selected. Modeless Show + PushFrame survives Hide/Show.
            var frame = new DispatcherFrame();
            window.Closed += (_, _) => frame.Continue = false;
            window.Show();
            Dispatcher.PushFrame(frame);
            return Result.Succeeded;
        }
        finally
        {
            composition.Coordinator.Complete();
        }
    }
}
