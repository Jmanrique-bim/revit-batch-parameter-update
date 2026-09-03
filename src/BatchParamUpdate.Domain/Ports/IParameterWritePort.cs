using BatchParamUpdate.Domain.Model;

namespace BatchParamUpdate.Domain.Ports;

public interface IParameterWritePort
{
    BatchExecutionResult? Execute(
        SelectionContext scope,
        ParameterCandidate targetParameter,
        string newValue,
        IProgress<BatchProgress> progress);
}
