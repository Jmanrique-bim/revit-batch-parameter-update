using BatchParamUpdate.Core;
using Xunit;

namespace BatchParamUpdate.Tests.Unit.Domain;

public sealed class SessionStoragePathsTests
{
    [Fact]
    public void Root_IsUnderLocalAppData_NotGetTempPath()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        Assert.Equal(Path.Combine(local, "juanManriqueHexagon"), SessionStoragePaths.Root);
        Assert.False(SessionStoragePaths.Root.StartsWith(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void FileNames_AreRunIdDashDocument_WithTxtAndJson()
    {
        Assert.Equal(
            Path.Combine(SessionStoragePaths.LogsDir, "abcd1234-Test_Doc.txt"),
            SessionStoragePaths.LogFile("abcd1234", "Test Doc"));
        Assert.Equal(
            Path.Combine(SessionStoragePaths.TrackerDir, "abcd1234-Test_Doc.json"),
            SessionStoragePaths.TrackerFile("abcd1234", "Test Doc"));
    }
}
