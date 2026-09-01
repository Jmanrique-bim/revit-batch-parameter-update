using System.Collections.Concurrent;
using BatchParamUpdate.Domain.ErrorCatalog;
using BatchParamUpdate.Domain.Ports;

namespace BatchParamUpdate.Core;

public sealed class SessionFileLogger : ILoggerPort, IDisposable
{
    private readonly BlockingCollection<string> _queue = new();
    private readonly Thread _writer;
    private readonly string _path;

    public SessionFileLogger(string runId, string documentName)
    {
        var dir = Path.Combine(Path.GetTempPath(), "juanManriqueHexagon", "LOGS");
        Directory.CreateDirectory(dir);
        var fileName = $"revit-{runId}-{DocumentNameSanitizer.Sanitize(documentName)}.txt";
        _path = Path.Combine(dir, fileName);
        _writer = new Thread(Drain) { IsBackground = true, Name = "SessionFileLogger" };
        _writer.Start();
    }

    public void Info(string message) => Enqueue("INFO", message);

    public void Warn(string message, WarningCode code)
        => Enqueue("WARN", $"{ErrorWarningCatalog.Code(code)} {message}");

    public void Error(string message, ErrorCode code)
        => Enqueue("ERROR", $"{ErrorWarningCatalog.Code(code)} {message}");

    public void CloseSession()
    {
        _queue.CompleteAdding();
        _writer.Join();
    }

    public void Dispose()
    {
        if (!_queue.IsAddingCompleted)
            CloseSession();
        _queue.Dispose();
    }

    private void Enqueue(string level, string message)
        => _queue.Add($"{DateTimeOffset.UtcNow:O} {level} {message}");

    private void Drain()
    {
        foreach (var line in _queue.GetConsumingEnumerable())
            File.AppendAllText(_path, line + Environment.NewLine);
    }
}
