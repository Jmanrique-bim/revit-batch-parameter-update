using BatchParamUpdate.Core;
using BatchParamUpdate.Domain.ErrorCatalog;
using BatchParamUpdate.Domain.Model;
using BatchParamUpdate.Domain.Ports;

namespace BatchParamUpdate.Application.UseCases;

public sealed class RunBatchUpdateUseCase
{
    private readonly IParameterWritePort _write;
    private readonly ILoggerPort? _logger;
    private readonly RecordSessionUseCase? _recorder;

    public RunBatchUpdateUseCase(
        IParameterWritePort write,
        ILoggerPort? logger = null,
        RecordSessionUseCase? recorder = null)
    {
        _write = write;
        _logger = logger;
        _recorder = recorder;
    }

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
        using var timer = PhaseTimer.Start();

        var result = operation.TargetParameter.Binding == ParameterBinding.Instance
            ? _write.ExecuteInstanceUpdate(scope, operation.TargetParameter, operation.NewValue)
            : _write.ExecuteTypeUpdate(scope, operation.TargetParameter, operation.NewValue);

        _recorder?.RecordPhaseTiming("Execution", timer.ElapsedMs);

        if (result is null)
        {
            Error = ErrorCode.DocumentNotModifiable;
            session.TransitionTo(SessionState.Blocked);
            _recorder?.End(session);
            return null;
        }

        LogSkips(result);
        _recorder?.RecordBatch(result, scope);
        Error = null;
        session.TransitionTo(SessionState.Completed);
        _recorder?.End(session);
        return result;
    }

    private void LogSkips(BatchExecutionResult result)
    {
        if (_logger is null || result.InstanceOutcome is null)
            return;

        foreach (var skip in result.InstanceOutcome.Skips)
            _logger.Warn($"{skip.Element.Id}: {skip.Message}", skip.Code);
    }
}
