namespace BatchParamUpdate.Domain.Model;

public sealed record ParameterCandidate(
    string Name,
    IReadOnlyList<ElementRef> SourceElementRefs,
    IReadOnlyList<string> ObservedValues,
    ParameterKey Key = default)
{
    public ParameterCandidate(string name, IReadOnlyList<ElementRef> sourceElementRefs)
        : this(name, sourceElementRefs, [], ParameterKey.ByName(name))
    {
    }

    /// <summary>Key with a guaranteed name fallback.</summary>
    public ParameterKey ResolvedKey => Key.Name is null ? ParameterKey.ByName(Name) : Key;

    internal static IReadOnlyList<ParameterCandidate> Deduplicate(IEnumerable<ParameterCandidate> candidates)
    {
        var map = new Dictionary<string, ParameterCandidate>(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in candidates)
        {
            if (map.TryGetValue(candidate.Name, out var existing))
            {
                var merged = existing.SourceElementRefs
                    .Concat(candidate.SourceElementRefs)
                    .Distinct()
                    .ToList();
                var values = existing.ObservedValues
                    .Concat(candidate.ObservedValues)
                    .Distinct(StringComparer.Ordinal)
                    .ToList();
                map[candidate.Name] = existing with
                {
                    SourceElementRefs = merged,
                    ObservedValues = values
                };
            }
            else
            {
                map[candidate.Name] = candidate;
            }
        }

        return map.Values.ToList();
    }
}
