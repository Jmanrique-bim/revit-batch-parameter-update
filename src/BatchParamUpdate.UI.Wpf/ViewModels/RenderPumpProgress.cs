using System.Windows.Threading;
using BatchParamUpdate.Domain.Model;

namespace BatchParamUpdate.UI.Wpf.ViewModels;

/// <summary>
/// Reports batch progress and forces a paint pass so the bar actually moves during the write.
///
/// The write loop runs on the Revit API thread (== the WPF UI thread) inside the ExternalEvent
/// bridge, which blocks the message pump. Pumping at <see cref="DispatcherPriority.Render"/>
/// repaints (Render / DataBind / Normal work runs) without draining <c>Input</c> or
/// <c>Background</c>, so a queued Run click or element pick cannot re-enter while the Revit
/// <c>Transaction</c> is open. (The old pump used <c>Background</c>, which sits below <c>Input</c>
/// and therefore did dispatch input — that was the reentrancy bug.)
///
/// ponytail: repaint throttled to ~30fps so a large batch isn't dominated by nested dispatcher
/// pumps; the final report always pumps so the bar reaches 100%.
/// </summary>
public sealed class RenderPumpProgress(Action<BatchProgress> onReport) : IProgress<BatchProgress>
{
    private long _lastPumpTicks;

    public void Report(BatchProgress value)
    {
        onReport(value);

        var now = Environment.TickCount64;
        if (now - _lastPumpTicks < 33 && value.Done < value.Total)
            return;

        _lastPumpTicks = now;
        Dispatcher.CurrentDispatcher.Invoke(static () => { }, DispatcherPriority.Render);
    }
}
