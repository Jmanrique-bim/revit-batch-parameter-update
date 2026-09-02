namespace BatchParamUpdate.Core;

public static class SessionStoragePaths
{
    public const string AppFolderName = "juanManriqueHexagon";

    public static string Root => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        AppFolderName);

    public static string LogsDir => Path.Combine(Root, "LOGS");

    public static string TrackerDir => Path.Combine(Root, "TRACKER");

    public static string LogFile(string runId, string documentName)
        => Path.Combine(LogsDir, $"{runId}-{DocumentNameSanitizer.Sanitize(documentName)}.txt");

    public static string TrackerFile(string runId, string documentName)
        => Path.Combine(TrackerDir, $"{runId}-{DocumentNameSanitizer.Sanitize(documentName)}.json");
}
