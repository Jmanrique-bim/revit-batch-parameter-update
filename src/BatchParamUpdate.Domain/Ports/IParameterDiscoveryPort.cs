using BatchParamUpdate.Domain.Model;

namespace BatchParamUpdate.Domain.Ports;

public interface IParameterDiscoveryPort
{
    ParameterCandidateSet Discover(SelectionContext scope);
}
