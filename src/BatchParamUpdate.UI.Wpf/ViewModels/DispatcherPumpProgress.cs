using System.Windows.Threading;
using BatchParamUpdate.Domain.Model;

namespace BatchParamUpdate.UI.Wpf.ViewModels;

/// <summary>
/// Reports batch progress and then lets the WPF message queue drain so the progress bar
/// actually repaints during the write.
///
/// The batch runs synchronously on the UI/Revit-API thread because the window is
/// modal (an ExternalEvent worker would not fire while a modal dialog is up, and the Revit API
/// cannot be called from a background thread). Pump at <see cref="DispatcherPriority.Render"/>
/// between elements so the bar moves without processing Input. After the write, drain Input
/// while Run/Select are still disabled so queued clicks no-op instead of starting a second
/// batch. <c>Application.Current</c> is null inside Revit. To go further, make the window
/// modeless and drive the write from an IExternalEventHandler.
/// </summary>
public sealed class DispatcherPumpProgress(Action<BatchProgress> onReport) : IProgress<BatchProgress>
{
    public void Report(BatchProgress value)
    {
        onReport(value);
        Dispatcher.CurrentDispatcher.Invoke(static () => { }, DispatcherPriority.Render);
    }

    public static void DrainQueuedInput()
        => Dispatcher.CurrentDispatcher.Invoke(static () => { }, DispatcherPriority.Input);
}
