namespace BatchParamUpdate.Domain.Model;

/// <summary>
/// The live text filter applied against the discovered instance-parameter candidate set.
/// </summary>
public sealed class ParameterSearch
{
    public ParameterSearch(ParameterCandidateSet set, string text = "")
    {
        Set = set;
        Text = text;
    }

    public ParameterCandidateSet Set { get; }

    public string Text { get; set; }

    public IReadOnlyList<ParameterCandidate> Matches
    {
        get
        {
            if (string.IsNullOrEmpty(Text))
                return Set.Candidates;

            return Set.Candidates
                .Where(c => c.Name.Contains(Text, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }
}
