using BatchParamUpdate.Domain.Model;
using Xunit;

namespace BatchParamUpdate.Tests.Unit.Domain;

public sealed class InstallerPackageTests
{
    [Fact]
    public void SupportedRevitYears_IsClosedList_2025_2026()
    {
        Assert.Equal([2025, 2026], InstallerPackage.SupportedRevitYears);
    }

    [Fact]
    public void OffersInstallFor_NeverIncludesYearsOutsideSupportedList()
    {
        var package = new InstallerPackage([2024, 2025, 2027, 2028]);

        Assert.False(package.OffersInstallFor(2024));
        Assert.False(package.OffersInstallFor(2027));
        Assert.False(package.OffersInstallFor(2028));
        Assert.True(package.OffersInstallFor(2025));
        Assert.DoesNotContain(2024, package.DetectedRevitYears);
        Assert.DoesNotContain(2027, package.DetectedRevitYears);
        Assert.DoesNotContain(2028, package.DetectedRevitYears);
        Assert.Empty(package.ActionsFor(2024));
        Assert.Equal(
            [InstallerAction.Install, InstallerAction.Update, InstallerAction.Uninstall],
            package.ActionsFor(2025));
    }
}
