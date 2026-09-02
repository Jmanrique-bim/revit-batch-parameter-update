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
        SelectionContext scope,
        IProgress<BatchProgress> progress)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(progress);

        if (!operation.HasReplacementValue)
        {
            Error = ErrorCode.EmptyValue;
            return null;
        }

        session.TransitionTo(SessionState.Executing);

        BatchExecutionResult? result;
        try
        {
            result = _write.Execute(scope, operation.TargetParameter, operation.NewValue, progress);
        }
        catch
        {
            // Same terminal as a null write result so the coordinator can copy Error, emit
            // FlowBlocked / StateChanged, and raise Changed. Rethrowing skipped that bookkeeping.
            result = null;
        }

        if (result is null)
        {
            Error = ErrorCode.DocumentNotModifiable;
            session.TransitionTo(SessionState.Blocked);
            return null;
        }

        if (result.RolledBack)
        {
            Error = ErrorCode.BatchRolledBack;
            session.TransitionTo(SessionState.Blocked);
            return result;
        }

        Error = null;
        session.TransitionTo(SessionState.AwaitingReplacementValue);
        return result;
    }
}
