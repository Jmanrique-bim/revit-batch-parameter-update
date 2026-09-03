namespace BatchParamUpdate.Domain.Model;

public sealed record ElementRef(string Id, string CategoryName, string TypeName = "")
{
    public string DisplayLabel =>
        string.IsNullOrEmpty(TypeName)
            ? (string.IsNullOrEmpty(CategoryName) ? Id : CategoryName)
            : $"{TypeName} ({CategoryName})";
}
