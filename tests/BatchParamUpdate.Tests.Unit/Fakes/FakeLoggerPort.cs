using BatchParamUpdate.Domain.ErrorCatalog;
using BatchParamUpdate.Domain.Ports;

namespace BatchParamUpdate.Tests.Unit.Fakes;

public sealed class FakeLoggerPort : ILoggerPort
{
    public List<string> Lines { get; } = [];
    public bool Closed { get; private set; }

    public void Info(string message) => Lines.Add($"INFO {message}");

    public void Warn(string message, WarningCode code)
        => Lines.Add($"WARN {code} {message}");

    public void Error(string message, ErrorCode code)
        => Lines.Add($"ERROR {code} {message}");

    public void CloseSession() => Closed = true;
}
