namespace BatchParamUpdate.Domain.Model;

public static class RevitAddinPaths
{
    /// <summary>
    /// The per-user Revit add-ins folder for a given year. This is the location Revit loads
    /// per-user manifests from and it needs no administrator rights, unlike the all-users
    /// (ProgramData / Program Files) locations.
    /// </summary>
    public static string PerUserAddinsFolder(int year) => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Autodesk",
        "Revit",
        "Addins",
        year.ToString());

    /// <summary>
    /// The legacy all-users add-ins folder (%ProgramData%). Earlier installer versions wrote
    /// here; installs are per-user now. Kept only so uninstall/repair can remove a stale copy
    /// that would otherwise make Revit load the command twice.
    /// </summary>
    public static string LegacyAllUsersAddinsFolder(int year) => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "Autodesk",
        "Revit",
        "Addins",
        year.ToString());
}
