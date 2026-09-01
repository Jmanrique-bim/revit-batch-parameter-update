using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using BatchParamUpdate.Domain.Model;
using BatchParamUpdate.Domain.Ports;

namespace BatchParamUpdate.Adapters.Revit.DialogSuppression;

public sealed class RevitDialogSuppressionPort : INativeDialogSuppressionPort
{
    private readonly UIApplication _uiapp;
    private readonly Document _doc;

    public RevitDialogSuppressionPort(UIApplication uiapp, Document doc)
    {
        _uiapp = uiapp;
        _doc = doc;
    }

    public WorkshareStatus GetWorkshareStatus(ElementRef element)
    {
        if (!_doc.IsWorkshared || !TryParseId(element.Id, out var id))
            return WorkshareStatus.NotWorkshared;

        return WorksharingUtils.GetCheckoutStatus(_doc, id) switch
        {
            CheckoutStatus.OwnedByCurrentUser => WorkshareStatus.OwnedByCurrentUser,
            CheckoutStatus.OwnedByOtherUser => WorkshareStatus.OwnedByOtherUser,
            _ => WorkshareStatus.NotWorkshared
        };
    }

    public IDisposable SuppressNativeDialogsDuringBatch()
    {
        _uiapp.DialogBoxShowing += OnDialogBoxShowing;
        return new SuppressionScope(_uiapp, OnDialogBoxShowing);
    }

    internal static IFailuresPreprocessor CreateFailuresPreprocessor()
        => new BatchFailuresPreprocessor();

    private static void OnDialogBoxShowing(object? sender, DialogBoxShowingEventArgs args)
    {
        try
        {
            args.OverrideResult((int)TaskDialogResult.Cancel);
        }
        catch (Autodesk.Revit.Exceptions.InvalidOperationException)
        {
            // ponytail: some native dialogs reject OverrideResult; the FailuresPreprocessor is the other layer.
        }
    }

    private static bool TryParseId(string id, out ElementId elementId)
    {
        if (long.TryParse(id, out var value))
        {
            elementId = new ElementId(value);
            return true;
        }

        elementId = ElementId.InvalidElementId;
        return false;
    }

    private sealed class SuppressionScope : IDisposable
    {
        private readonly UIApplication _uiapp;
        private readonly EventHandler<DialogBoxShowingEventArgs> _handler;
        private bool _disposed;

        public SuppressionScope(UIApplication uiapp, EventHandler<DialogBoxShowingEventArgs> handler)
        {
            _uiapp = uiapp;
            _handler = handler;
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _uiapp.DialogBoxShowing -= _handler;
            _disposed = true;
        }
    }

    private sealed class BatchFailuresPreprocessor : IFailuresPreprocessor
    {
        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            foreach (var message in failuresAccessor.GetFailureMessages().ToList())
            {
                if (message.GetSeverity() == FailureSeverity.Warning)
                    failuresAccessor.DeleteWarning(message);
            }

            return FailureProcessingResult.Continue;
        }
    }
}
