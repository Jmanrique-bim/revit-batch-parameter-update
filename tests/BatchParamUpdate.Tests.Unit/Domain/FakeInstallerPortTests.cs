using BatchParamUpdate.Tests.Unit.Fakes;
using Xunit;

namespace BatchParamUpdate.Tests.Unit.Domain;

public sealed class FakeInstallerPortTests
{
    [Fact]
    public void IsAddinInstalled_TracksInstallUpdateUninstall()
    {
        var port = new FakeInstallerPort();
        Assert.False(port.IsAddinInstalled(2025));

        port.Install(2025);
        Assert.True(port.IsAddinInstalled(2025));

        port.Update(2025);
        Assert.True(port.IsAddinInstalled(2025));

        port.Uninstall(2025);
        Assert.False(port.IsAddinInstalled(2025));
    }
}
