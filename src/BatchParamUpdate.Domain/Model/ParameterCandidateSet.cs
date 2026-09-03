namespace BatchParamUpdate.Domain.Model;

/// <summary>
/// The distinct writable, text-typed instance parameters present on at least one element in the
/// current selection scope, deduplicated to one entry each regardless of how many elements share
/// them.
/// </summary>
public sealed class ParameterCandidateSet
{
    public IReadOnlyList<ParameterCandidate> Candidates { get; }

    public ParameterCandidateSet(IEnumerable<ParameterCandidate> candidates)
        => Candidates = ParameterCandidate.Deduplicate(candidates);
}
