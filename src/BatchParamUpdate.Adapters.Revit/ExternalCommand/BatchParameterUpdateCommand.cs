using System.Windows.Interop;
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
    // The window is modeless, so a second ribbon click while it is open just refocuses it.
    private static MainWindow? _open;

    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        if (_open is not null)
        {
            _open.Activate();
            return Result.Succeeded;
        }

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
        // Lifetime is the window's now, not this method's — disposed in the Closed handler.
        var logger = new SessionFileLogger(runId, documentName);

        MainWindow? window = null;
        var composition = CompositionRoot.Build(
            uiapp,
            doc,
            runId,
            documentName,
            logger,
            hideHost: () => window?.Hide(),
            showHost: () => window?.Show());

        window = new MainWindow();
        new WindowInteropHelper(window).Owner = uiapp.MainWindowHandle;
        window.Bind(composition.View);

        // Closed fires on the WPF loop, outside a Revit API context — keep only non-API work here.
        // If the document is closed while the window is open, the write is refused by the
        // `IsValidObject` guard in CompositionRoot and the VM shows an error; the window is not
        // auto-closed (subscribing Application.DocumentClosing would need an API context to
        // unsubscribe on normal close, and leaking the handler holds the document).
        window.Closed += (_, _) =>
        {
            _open = null;
            try
            {
                composition.Coordinator.Complete();
            }
            finally
            {
                logger.Dispose();
                try
                {
                    composition.RevitBridge.Dispose();
                }
                catch (Autodesk.Revit.Exceptions.InvalidOperationException)
                {
                    // Not in an API context on this close; Revit releases the ExternalEvent on shutdown.
                }
            }
        };

        _open = window;
        window.Show();
        return Result.Succeeded;
    }
}
