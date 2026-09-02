namespace BatchParamUpdate.Domain.Model;

public sealed record ParameterCandidate(
    string Name,
    ParameterBinding Binding,
    IReadOnlyList<ElementRef> SourceElementRefs,
    IReadOnlyList<string> ObservedValues)
{
    public ParameterCandidate(
        string name,
        ParameterBinding binding,
        IReadOnlyList<ElementRef> sourceElementRefs)
        : this(name, binding, sourceElementRefs, [])
    {
    }

    internal static IReadOnlyList<ParameterCandidate> Deduplicate(
        IEnumerable<ParameterCandidate> candidates,
        ParameterBinding binding)
    {
        var map = new Dictionary<string, ParameterCandidate>(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in candidates)
        {
            if (candidate.Binding != binding)
                continue;

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
