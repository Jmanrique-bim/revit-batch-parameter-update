using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using BatchParamUpdate.Domain.Ports;

namespace BatchParamUpdate.Adapters.Revit.DialogSuppression;

public sealed class RevitDialogSuppressionPort : INativeDialogSuppressionPort
{
    private readonly UIApplication _uiapp;

    public RevitDialogSuppressionPort(UIApplication uiapp) => _uiapp = uiapp;

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
