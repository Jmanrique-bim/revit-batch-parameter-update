using Autodesk.Revit.UI;

namespace BatchParamUpdate.Adapters.Revit.ExternalEvents;

/// <summary>
/// Marshals a delegate onto Revit's API thread for the modeless window. A modeless add-in can
/// only touch the Revit API (open a <c>Transaction</c>) from inside an ExternalEvent callback,
/// so the batch write is routed through here.
///
/// ponytail: single-slot, not a queue — the Run button is disabled while a batch runs, so only
/// one job is ever in flight. Add a queue only if that stops being true.
/// </summary>
public sealed class RevitApiEventBridge : IExternalEventHandler, IDisposable
{
    private readonly ExternalEvent _event;
    private Action? _pending;
    private TaskCompletionSource<object?>? _tcs;

    /// <summary>Must be constructed from a valid Revit API context (e.g. an IExternalCommand).</summary>
    public RevitApiEventBridge() => _event = ExternalEvent.Create(this);

    /// <summary>Runs <paramref name="work"/> on the Revit API thread; completes when it returns.</summary>
    public Task RunAsync(Action work)
    {
        _pending = work;
        _tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _event.Raise();
        return _tcs.Task;
    }

    public void Execute(UIApplication app)
    {
        var work = _pending;
        var tcs = _tcs;
        _pending = null;
        _tcs = null;
        try
        {
            work?.Invoke();
            tcs?.SetResult(null);
        }
        catch (Exception ex)
        {
            tcs?.SetException(ex);
        }
    }

    public string GetName() => "BatchParamUpdate.RevitApiEventBridge";

    public void Dispose() => _event.Dispose();
}
