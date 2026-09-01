using BatchParamUpdate.Domain.ErrorCatalog;

namespace BatchParamUpdate.Domain.Ports;

public interface ILoggerPort
{
    void Info(string message);
    void Warn(string message, WarningCode code);
    void Error(string message, ErrorCode code);
    void CloseSession();
}
