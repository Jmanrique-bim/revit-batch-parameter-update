using BatchParamUpdate.Domain.ErrorCatalog;
using BatchParamUpdate.Domain.Ports;

namespace BatchParamUpdate.Core;

public sealed class SessionFileLogger : ILoggerPort, IDisposable
{
    public SessionFileLogger(string runId, string documentName)
    {
        Directory.CreateDirectory(SessionStoragePaths.LogsDir);
        FilePath = SessionStoragePaths.LogFile(runId, documentName);
        Info(SessionTrace.Line("cmd", "log", "open", ("path", FilePath)));
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

    // IPX _full.log layout (${longdate}\t${level}\t${message}); no NLog.
    // Swallow IO: recording must not fail the batch (HOW_TO_SESSIONS).
    private void Write(string level, string message)
    {
        try
        {
            File.AppendAllText(
                FilePath,
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.ffff}\t{level}\t{message}{Environment.NewLine}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }
}
