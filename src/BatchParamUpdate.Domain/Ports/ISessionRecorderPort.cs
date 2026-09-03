using BatchParamUpdate.Domain.Model;

namespace BatchParamUpdate.Domain.Ports;

public interface ISessionRecorderPort
{
    void Record(MetricsRecord record);
}
