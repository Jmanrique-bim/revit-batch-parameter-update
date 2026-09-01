using BatchParamUpdate.Domain.Model;

namespace BatchParamUpdate.Domain.Ports;

public interface IParameterWritePort
{
    BatchExecutionResult? ExecuteInstanceUpdate(
        SelectionContext scope,
        ParameterCandidate targetParameter,
        string newValue);

    BatchExecutionResult? ExecuteTypeUpdate(
        SelectionContext scope,
        ParameterCandidate targetParameter,
        string newValue);
}
