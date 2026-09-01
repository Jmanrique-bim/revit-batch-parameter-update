namespace BatchParamUpdate.Domain.Ports;

public interface IInstallerPort
{
    IReadOnlyList<int> DetectInstalledRevitYears();

    void Install(int revitYear);

    void Update(int revitYear);

    void Uninstall(int revitYear);
}
