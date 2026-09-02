using System.Windows.Interop;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
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

        // Modeless: the window would otherwise outlive its document. Close it when that document
        // closes so a later Run can't target a disposed Document.
        // ponytail: DocumentClosing is cancelable; if the user then cancels the close we've still
        // shut our window. Acceptable — reopen the tool. DocumentClosed doesn't hand back the
        // Document to compare, so we key off Closing.
        void OnDocumentClosing(object? sender, DocumentClosingEventArgs e)
        {
            if (ReferenceEquals(e.Document, doc))
                window?.Close();
        }

        uiapp.Application.DocumentClosing += OnDocumentClosing;
        window.Closed += (_, _) =>
        {
            uiapp.Application.DocumentClosing -= OnDocumentClosing;
            try
            {
                composition.Coordinator.Complete();
            }
            finally
            {
                composition.RevitBridge.Dispose();
                logger.Dispose();
                _open = null;
            }
        };

        _open = window;
        window.Show();
        return Result.Succeeded;
    }
}
