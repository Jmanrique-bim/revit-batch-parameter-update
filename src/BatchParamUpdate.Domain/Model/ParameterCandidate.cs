namespace BatchParamUpdate.Domain.Model;

public sealed record ParameterCandidate(
    string Name,
    ParameterBinding Binding,
    IReadOnlyList<ElementRef> SourceElementRefs)
{
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
                map[candidate.Name] = existing with { SourceElementRefs = merged };
            }
            else
            {
                map[candidate.Name] = candidate;
            }
        }

        return map.Values.ToList();
    }
}
