using BatchParamUpdate.Domain.Model;
using BatchParamUpdate.Domain.Ports;

namespace BatchParamUpdate.Tests.Unit.Fakes;

public sealed class FakeSessionRecorderPort : ISessionRecorderPort
{
    public List<MetricsRecord> Records { get; } = [];

    public void Record(MetricsRecord record) => Records.Add(record);
}
