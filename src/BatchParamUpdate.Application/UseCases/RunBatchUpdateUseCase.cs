using BatchParamUpdate.Domain.ErrorCatalog;
using BatchParamUpdate.Domain.Model;
using BatchParamUpdate.Domain.Ports;

namespace BatchParamUpdate.Application.UseCases;

public sealed class RunBatchUpdateUseCase
{
    private readonly IParameterWritePort _write;

    public RunBatchUpdateUseCase(IParameterWritePort write) => _write = write;

    public ErrorCode? Error { get; private set; }

    public BatchExecutionResult? Execute(
        Session session,
        ReplacementOperation operation,
        SelectionContext scope)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(scope);

        if (!operation.HasReplacementValue)
        {
            Error = ErrorCode.EmptyValue;
            return null;
        }

        session.TransitionTo(SessionState.Executing);

        var result = operation.TargetParameter.Binding == ParameterBinding.Instance
            ? _write.ExecuteInstanceUpdate(scope, operation.TargetParameter, operation.NewValue)
            : _write.ExecuteTypeUpdate(scope, operation.TargetParameter, operation.NewValue);

        if (result is null)
        {
            Error = ErrorCode.DocumentNotModifiable;
            session.TransitionTo(SessionState.Blocked);
            return null;
        }

        Error = null;
        session.TransitionTo(SessionState.Completed);
        return result;
    }
}
