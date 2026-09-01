using BatchParamUpdate.Domain.Model;
using BatchParamUpdate.Domain.Ports;

namespace BatchParamUpdate.Tests.Unit.Fakes;

public sealed class FakeParameterDiscoveryPort : IParameterDiscoveryPort
{
    public InstanceParameterCandidateSet Instance { get; set; } = new([]);

    public TypeParameterCandidateSet Type { get; set; } = new([]);

    public InstanceParameterCandidateSet DiscoverInstanceCandidates(SelectionContext scope) => Instance;

    public TypeParameterCandidateSet DiscoverTypeCandidates(SelectionContext scope) => Type;
}
