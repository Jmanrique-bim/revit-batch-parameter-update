using System.IO;
using BatchParamUpdate.Domain.Model;
using BatchParamUpdate.Domain.Ports;
using Microsoft.Win32;

namespace BatchParamUpdate.Installer;

public sealed class RevitInstallerAdapter : IInstallerPort
{
    public IReadOnlyList<int> DetectInstalledRevitYears()
        => InstallerPackage.SupportedRevitYears.Where(YearIsInstalled).ToArray();

    public bool IsAddinInstalled(int revitYear)
    {
        EnsureSupported(revitYear);
        return File.Exists(Path.Combine(AddinsFolder(revitYear), $"BatchParamUpdate.Adapters.Revit.{revitYear}.addin"));
    }

    public void Install(int revitYear)
    {
        EnsureSupported(revitYear);
        var source = PayloadDirectory(revitYear);
        if (!Directory.Exists(source))
            throw new DirectoryNotFoundException($"Add-in payload for Revit {revitYear} was not packaged.");

        var dest = AddinsFolder(revitYear);
        var payloadDest = Path.Combine(dest, "BatchParamUpdate");
        Directory.CreateDirectory(payloadDest);

        foreach (var addin in Directory.GetFiles(source, "*.addin"))
            File.Copy(addin, Path.Combine(dest, Path.GetFileName(addin)), overwrite: true);

        var payloadSource = Path.Combine(source, "BatchParamUpdate");
        if (Directory.Exists(payloadSource))
        {
            foreach (var file in Directory.GetFiles(payloadSource, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(payloadSource, file);
                var target = Path.Combine(payloadDest, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(file, target, overwrite: true);
            }
        }
    }

    public void Update(int revitYear) => Install(revitYear);

    public void Uninstall(int revitYear)
    {
        EnsureSupported(revitYear);
        var dest = AddinsFolder(revitYear);
        var addin = Path.Combine(dest, $"BatchParamUpdate.Adapters.Revit.{revitYear}.addin");
        var payload = Path.Combine(dest, "BatchParamUpdate");
        if (File.Exists(addin))
            File.Delete(addin);
        if (Directory.Exists(payload))
            Directory.Delete(payload, recursive: true);
    }

    internal static string AddinsFolder(int year) => RevitAddinPaths.PerUserAddinsFolder(year);

    private static void EnsureSupported(int year)
    {
        if (!InstallerPackage.SupportedRevitYears.Contains(year))
            throw new ArgumentOutOfRangeException(nameof(year), year, "This installer only supports Revit 2025 and 2026.");
    }

    private static string PayloadDirectory(int year)
        => Path.Combine(AppContext.BaseDirectory, "addins", year.ToString());

    private static bool YearIsInstalled(int year)
    {
        foreach (var hive in new[] { $@"SOFTWARE\Autodesk\Revit\{year}", $@"SOFTWARE\WOW6432Node\Autodesk\Revit\{year}" })
        {
            using var key = Registry.LocalMachine.OpenSubKey(hive);
            if (key is not null)
                return true;
        }

        return false;
    }
}
