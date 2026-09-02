using BatchParamUpdate.Domain.ErrorCatalog;
using BatchParamUpdate.Domain.Ports;

namespace BatchParamUpdate.Core;

public sealed class SessionFileLogger : ILoggerPort, IDisposable
{
    public SessionFileLogger(string runId, string documentName)
    {
        var dir = Path.Combine(Path.GetTempPath(), "juanManriqueHexagon", "LOGS");
        Directory.CreateDirectory(dir);
        FilePath = Path.Combine(dir, $"revit-{runId}-{DocumentNameSanitizer.Sanitize(documentName)}_full.log");
        Info($"Creating log at: {FilePath}");
    }

    public string FilePath { get; }

    public void Info(string message) => Write("Info", message);

    public void Warn(string message, WarningCode code)
        => Write("Warn", $"{ErrorWarningCatalog.Code(code)} {message}");

    public void Error(string message, ErrorCode code)
        => Write("Error", $"{ErrorWarningCatalog.Code(code)} {message}");

    public void CloseSession()
    {
    }

    public void Dispose()
    {
    }

    // ponytail: IPX _full.log layout (${longdate}\t${level}\t${message}); no NLog.
    private void Write(string level, string message)
        => File.AppendAllText(
            FilePath,
            $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.ffff}\t{level}\t{message}{Environment.NewLine}");
}
