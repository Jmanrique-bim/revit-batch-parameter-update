using BatchParamUpdate.Domain.Model;
using BatchParamUpdate.Domain.Ports;

namespace BatchParamUpdate.Tests.Unit.Fakes;

public sealed class FakeParameterDiscoveryPort : IParameterDiscoveryPort
{
    public ParameterCandidateSet Set { get; set; } = new([]);

    public ParameterCandidateSet Discover(SelectionContext scope) => Set;
}
