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
}
