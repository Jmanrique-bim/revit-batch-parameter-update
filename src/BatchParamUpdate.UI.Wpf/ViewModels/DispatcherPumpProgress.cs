using System.Windows.Threading;
using BatchParamUpdate.Domain.Model;

namespace BatchParamUpdate.UI.Wpf.ViewModels;

/// <summary>
/// Reports batch progress and then lets the WPF message queue drain so the progress bar
/// actually repaints during the write.
///
/// The batch runs synchronously on the UI/Revit-API thread because the window is
/// modal (an ExternalEvent worker would not fire while a modal dialog is up, and the Revit API
/// cannot be called from a background thread). Pumping the dispatcher between elements is the
/// cheap way to keep the bar moving. Use <see cref="Dispatcher.CurrentDispatcher"/> at
/// <see cref="DispatcherPriority.Render"/> — <c>Application.Current</c> is null inside Revit, and
/// Background would drain Input so a second Run or Select Elements click re-enters while the
/// transaction is open. To go further, make the window modeless and drive the write from an
/// IExternalEventHandler.
/// </summary>
public sealed class DispatcherPumpProgress(Action<BatchProgress> onReport) : IProgress<BatchProgress>
{
    public void Report(BatchProgress value)
    {
        onReport(value);
        Dispatcher.CurrentDispatcher.Invoke(static () => { }, DispatcherPriority.Render);
    }
}
