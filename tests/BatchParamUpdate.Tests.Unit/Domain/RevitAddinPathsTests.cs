using BatchParamUpdate.Domain.Model;
using Xunit;

namespace BatchParamUpdate.Tests.Unit.Domain;

public sealed class RevitAddinPathsTests
{
    [Theory]
    [InlineData(2025)]
    [InlineData(2026)]
    public void PerUserAddinsFolder_IsUnderAppData_NotAnAdminLocation(int year)
    {
        var path = RevitAddinPaths.PerUserAddinsFolder(year);
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

        Assert.StartsWith(appData, path, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(programFiles, path, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(programData, path, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith(Path.Combine("Autodesk", "Revit", "Addins", year.ToString()), path);
    }

    [Theory]
    [InlineData(2025)]
    [InlineData(2026)]
    public void LegacyAllUsersAddinsFolder_IsUnderProgramData(int year)
    {
        var path = RevitAddinPaths.LegacyAllUsersAddinsFolder(year);
        var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

        Assert.StartsWith(programData, path, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith(Path.Combine("Autodesk", "Revit", "Addins", year.ToString()), path);
    }
}
