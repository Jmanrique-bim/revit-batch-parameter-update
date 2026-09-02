using System.Text;
using BatchParamUpdate.Core;
using BatchParamUpdate.Domain.Model;
using BatchParamUpdate.Domain.Ports;

namespace BatchParamUpdate.Adapters.Persistence;

/// <summary>
/// Writes the skip list from a batch run to a CSV file under the same
/// %TEMP%\juanManriqueHexagon root used by the logger and the session
/// recorder, so all three artifacts of one run live side by side.
/// </summary>
public sealed class CsvSkipReportExporter : IReportExportPort
{
    private static readonly string[] Header = ["Element", "Category", "Reason", "Message"];

    public string ExportSkips(IReadOnlyList<ElementSkip> skips, string runId)
    {
        var dir = Path.Combine(Path.GetTempPath(), "juanManriqueHexagon", "REPORTS");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"skip-report-{DocumentNameSanitizer.Sanitize(runId)}.csv");

        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", Header));
        foreach (var skip in skips)
        {
            sb.AppendLine(string.Join(
                ",",
                Csv(skip.Element.DisplayLabel),
                Csv(skip.Element.CategoryName),
                Csv(skip.Reason.ToString()),
                Csv(skip.Message)));
        }

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        return path;
    }

    private static string Csv(string value)
    {
        if (value.IndexOfAny([',', '"', '\n', '\r']) < 0)
            return value;

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
