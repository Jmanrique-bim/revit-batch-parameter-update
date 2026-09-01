using BatchParamUpdate.Domain.Model;
using BatchParamUpdate.Domain.Ports;

namespace BatchParamUpdate.Tests.Unit.Fakes;

public sealed class FakeSessionRecorderPort : ISessionRecorderPort
{
    public List<MetricsRecord> Records { get; } = [];

    public Exception? ThrowOnRecord { get; set; }

    public void Record(MetricsRecord record)
    {
        if (ThrowOnRecord is not null)
            throw ThrowOnRecord;

        Records.Add(record);
    }
}
