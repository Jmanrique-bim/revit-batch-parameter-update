namespace BatchParamUpdate.Domain.Model;

public sealed class TypeParameterCandidateSet
{
    public ParameterBinding Binding => ParameterBinding.Type;

    public IReadOnlyList<ParameterCandidate> Candidates { get; }

    public TypeParameterCandidateSet(IEnumerable<ParameterCandidate> candidates)
        => Candidates = ParameterCandidate.Deduplicate(candidates, ParameterBinding.Type);
}
