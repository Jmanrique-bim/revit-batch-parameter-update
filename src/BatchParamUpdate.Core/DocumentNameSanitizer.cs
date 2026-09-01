namespace BatchParamUpdate.Core;

public static class DocumentNameSanitizer
{
    public static string Sanitize(string title)
    {
        ArgumentNullException.ThrowIfNull(title);

        var sanitized = title.Replace(' ', '_');
        foreach (var c in Path.GetInvalidFileNameChars())
            sanitized = sanitized.Replace(c, '_');

        return sanitized.Length <= 60 ? sanitized : sanitized[..60];
    }
}
