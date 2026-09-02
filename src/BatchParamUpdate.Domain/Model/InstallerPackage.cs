namespace BatchParamUpdate.Domain.Model;

public enum InstallerAction
{
    Install,
    Update,
    Uninstall
}

public sealed class InstallerPackage
{
    public static readonly IReadOnlyList<int> SupportedRevitYears = [2025, 2026];

    public InstallerPackage(IReadOnlyList<int> detectedRevitYears)
    {
        DetectedRevitYears = detectedRevitYears
            .Where(year => SupportedRevitYears.Contains(year))
            .Distinct()
            .OrderBy(year => year)
            .ToArray();
    }

    public IReadOnlyList<int> DetectedRevitYears { get; }

    public IReadOnlyList<InstallerAction> ActionsFor(int year)
        => SupportedRevitYears.Contains(year) && DetectedRevitYears.Contains(year)
            ? [InstallerAction.Install, InstallerAction.Update, InstallerAction.Uninstall]
            : [];

    public bool OffersInstallFor(int year) => ActionsFor(year).Contains(InstallerAction.Install);
}
