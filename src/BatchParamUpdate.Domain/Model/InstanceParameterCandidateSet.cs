namespace BatchParamUpdate.Domain.Model;

public sealed class InstanceParameterCandidateSet
{
    public ParameterBinding Binding => ParameterBinding.Instance;

    public IReadOnlyList<ParameterCandidate> Candidates { get; }

    public InstanceParameterCandidateSet(IEnumerable<ParameterCandidate> candidates)
        => Candidates = ParameterCandidate.Deduplicate(candidates, ParameterBinding.Instance);
}
