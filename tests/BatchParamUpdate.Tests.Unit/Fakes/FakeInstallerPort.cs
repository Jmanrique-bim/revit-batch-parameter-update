using BatchParamUpdate.Domain.Ports;

namespace BatchParamUpdate.Tests.Unit.Fakes;

public sealed class FakeInstallerPort : IInstallerPort
{
    public List<int> InstalledYears { get; } = [];

    public List<int> InstallCalls { get; } = [];

    public List<int> UpdateCalls { get; } = [];

    public List<int> UninstallCalls { get; } = [];

    public HashSet<int> PackagedAddins { get; } = [];

    public IReadOnlyList<int> DetectInstalledRevitYears() => InstalledYears;

    public bool IsAddinInstalled(int revitYear) => PackagedAddins.Contains(revitYear);

    public void Install(int revitYear)
    {
        InstallCalls.Add(revitYear);
        PackagedAddins.Add(revitYear);
    }

    public void Update(int revitYear)
    {
        UpdateCalls.Add(revitYear);
        PackagedAddins.Add(revitYear);
    }

    public void Uninstall(int revitYear)
    {
        UninstallCalls.Add(revitYear);
        PackagedAddins.Remove(revitYear);
    }
}
