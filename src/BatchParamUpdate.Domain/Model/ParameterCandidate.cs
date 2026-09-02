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
        // Key on identity, not just display name: two elements can carry different parameters
        // that share a name (a built-in vs. a project/shared param). Merging those would make
        // the write target the first one's built-in id / GUID on every element.
        var map = new Dictionary<(string Name, int? BuiltInId, Guid? SharedGuid), ParameterCandidate>();
        foreach (var candidate in candidates)
        {
            var key = (candidate.Name.ToLowerInvariant(), candidate.ResolvedKey.BuiltInId, candidate.ResolvedKey.SharedGuid);
            if (map.TryGetValue(key, out var existing))
            {
                var merged = existing.SourceElementRefs
                    .Concat(candidate.SourceElementRefs)
                    .Distinct()
                    .ToList();
                var values = existing.ObservedValues
                    .Concat(candidate.ObservedValues)
                    .Distinct(StringComparer.Ordinal)
                    .ToList();
                map[key] = existing with
                {
                    SourceElementRefs = merged,
                    ObservedValues = values
                };
            }
            else
            {
                map[key] = candidate;
            }
        }

        return map.Values.ToList();
    }
}
