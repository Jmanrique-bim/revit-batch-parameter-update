using BatchParamUpdate.Domain.Model;
using BatchParamUpdate.Domain.Ports;

namespace BatchParamUpdate.Tests.Unit.Fakes;

public sealed class FakeNativeDialogSuppressionPort : INativeDialogSuppressionPort
{
    public Dictionary<string, WorkshareStatus> StatusByElementId { get; } = new();

    public int SuppressCalls { get; private set; }

    public WorkshareStatus GetWorkshareStatus(ElementRef element)
        => StatusByElementId.TryGetValue(element.Id, out var status)
            ? status
            : WorkshareStatus.NotWorkshared;

    public IDisposable SuppressNativeDialogsDuringBatch()
    {
        SuppressCalls++;
        return new Noop();
    }

    private sealed class Noop : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
