using BatchParamUpdate.Domain.Model;

namespace BatchParamUpdate.Domain.Ports;

public interface IParameterDiscoveryPort
{
    InstanceParameterCandidateSet DiscoverInstanceCandidates(SelectionContext scope);

    TypeParameterCandidateSet DiscoverTypeCandidates(SelectionContext scope);
}
