namespace BatchParamUpdate.Domain.Model;

public sealed class SharedSearchQuery
{
    public SharedSearchQuery(
        InstanceParameterCandidateSet instanceSet,
        TypeParameterCandidateSet typeSet,
        string text = "")
    {
        InstanceSet = instanceSet;
        TypeSet = typeSet;
        Text = text;
    }

    public InstanceParameterCandidateSet InstanceSet { get; }

    public TypeParameterCandidateSet TypeSet { get; }

    public string Text { get; set; }

    public IReadOnlyList<ParameterCandidate> MatchesInstanceSet => Filter(InstanceSet.Candidates);

    public IReadOnlyList<ParameterCandidate> MatchesTypeSet => Filter(TypeSet.Candidates);

    private IReadOnlyList<ParameterCandidate> Filter(IReadOnlyList<ParameterCandidate> candidates)
    {
        if (string.IsNullOrEmpty(Text))
            return candidates;

        return candidates
            .Where(c => c.Name.Contains(Text, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
