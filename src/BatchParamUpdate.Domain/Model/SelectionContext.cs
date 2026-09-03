namespace BatchParamUpdate.Domain.Model;

public sealed record SelectionContext(
    IReadOnlyList<ElementRef> ElementRefs,
    SelectionOrigin Origin)
{
    public bool IsValid => ElementRefs.Count > 0;
}
