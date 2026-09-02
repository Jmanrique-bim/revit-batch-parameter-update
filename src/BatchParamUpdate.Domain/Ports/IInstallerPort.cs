namespace BatchParamUpdate.Domain.Ports;

public interface IInstallerPort
{
    IReadOnlyList<int> DetectInstalledRevitYears();

    bool IsAddinInstalled(int revitYear);

    void Install(int revitYear);

    void Update(int revitYear);

    void Uninstall(int revitYear);
}
