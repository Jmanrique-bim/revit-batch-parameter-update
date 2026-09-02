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
            _logger?.Error(ErrorWarningCatalog.Message(ErrorCode.EmptyValue), ErrorCode.EmptyValue);
            return null;
        }

        session.TransitionTo(SessionState.Executing);
        _logger?.Info(
            $"Execute {operation.TargetParameter.Binding} {operation.TargetParameter.Name}='{operation.NewValue}' scope={scope.ElementRefs.Count}");
        using var timer = PhaseTimer.Start();

        var result = operation.TargetParameter.Binding == ParameterBinding.Instance
            ? _write.ExecuteInstanceUpdate(scope, operation.TargetParameter, operation.NewValue)
            : _write.ExecuteTypeUpdate(scope, operation.TargetParameter, operation.NewValue);

        _recorder?.RecordPhaseTiming("Execution", timer.ElapsedMs);

        if (result is null)
        {
            Error = ErrorCode.DocumentNotModifiable;
            session.TransitionTo(SessionState.Blocked);
            _logger?.Error(
                ErrorWarningCatalog.Message(ErrorCode.DocumentNotModifiable),
                ErrorCode.DocumentNotModifiable);
            _recorder?.End(session);
            return null;
        }

        LogOutcome(result, scope);
        _recorder?.RecordBatch(result, scope);
        Error = null;
        session.TransitionTo(SessionState.Completed);
        _recorder?.End(session);
        return result;
    }

    private void LogOutcome(BatchExecutionResult result, SelectionContext scope)
    {
        if (_logger is null)
            return;

        if (result.InstanceOutcome is { } instance)
        {
            var skippedIds = instance.Skips.Select(s => s.Element.Id).ToHashSet(StringComparer.Ordinal);
            foreach (var element in scope.ElementRefs.Where(e => !skippedIds.Contains(e.Id)))
                _logger.Info($"Updated {element.DisplayLabel}");
            foreach (var skip in instance.Skips)
                _logger.Warn($"{skip.Element.DisplayLabel}: {skip.Message}", skip.Code);
            return;
        }

        var type = result.TypeOutcome!;
        foreach (var affected in type.AffectedTypes)
            _logger.Info($"Type updated {affected.Name} from {affected.SourceElementRefs.Count} selected");
        _logger.Info($"Type path total elements {type.TotalElementsUpdated}");
    }
}
