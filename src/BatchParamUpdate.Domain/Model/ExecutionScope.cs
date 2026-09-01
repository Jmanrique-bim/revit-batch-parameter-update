namespace BatchParamUpdate.Domain.Model;

public sealed record ResolvedType(
    string Id,
    string Name,
    IReadOnlyList<ElementRef> SourceElementRefs);

public abstract record ExecutionScope;

public sealed record InstanceScope(SelectionContext Selection) : ExecutionScope;

public sealed record TypeScope(IReadOnlyList<ResolvedType> Types) : ExecutionScope;
