using BatchParamUpdate.Core;
using BatchParamUpdate.Domain.ErrorCatalog;
using BatchParamUpdate.Domain.Model;
using BatchParamUpdate.Domain.Ports;

namespace BatchParamUpdate.Application.Observability;

/// <summary>
/// The single place that turns <see cref="WorkflowEvent"/>s into the human-readable session log
/// and the NDJSON metrics record. Nothing else writes to either sink.
/// </summary>
public sealed class SessionTraceListener : IWorkflowObserver
{
    private readonly ISessionRecorderPort _recorder;
    private readonly ILoggerPort _logger;
    private readonly SessionRecord _identity;
    private BatchExecutionResult? _lastBatch;

    public SessionTraceListener(ISessionRecorderPort recorder, ILoggerPort logger, SessionRecord identity)
    {
        _recorder = recorder;
        _logger = logger;
        _identity = identity;
    }

    public void On(WorkflowEvent workflowEvent)
    {
        switch (workflowEvent)
        {
            case WorkflowEvent.SessionStarted:
                Record(new SessionStart(_identity.SessionId, DateTimeOffset.UtcNow));
                break;

            case WorkflowEvent.SelectionEstablished e:
                _logger.Info(SessionTrace.Line("ui", "select", "ready", ("origin", e.Origin), ("count", e.Count)));
                break;

            case WorkflowEvent.ParametersDiscovered e:
                Record(new PhaseTiming(_identity.SessionId, DateTimeOffset.UtcNow, "Discovery", e.ElapsedMs));
                _logger.Info(SessionTrace.Line("model", "discovery", "done", ("candidates", e.Count)));
                break;

            case WorkflowEvent.SearchRan e:
                Record(new SearchPerformed(_identity.SessionId, DateTimeOffset.UtcNow, e.Text, e.Matches));
                break;

            case WorkflowEvent.ParameterChosen e:
                Record(new ParameterSelected(_identity.SessionId, DateTimeOffset.UtcNow, e.Name));
                break;

            case WorkflowEvent.BatchStarting e:
                _logger.Info(SessionTrace.Line(
                    "model", "run", "start", ("name", e.ParameterName), ("value", e.NewValue), ("scope", e.ScopeCount)));
                break;

            case WorkflowEvent.BatchFinished e:
                Record(new PhaseTiming(_identity.SessionId, DateTimeOffset.UtcNow, "Execution", e.ElapsedMs));
                _lastBatch = e.Result;
                Record(Aggregate(e.Result, e.Scope));
                LogBatchOutcome(e.Result, e.Scope);
                break;

            case WorkflowEvent.FlowBlocked e:
                _logger.Error(ErrorWarningCatalog.Message(e.Code), e.Code);
                break;

            case WorkflowEvent.StateChanged e:
                _logger.Info(SessionTrace.Line(
                    "model", "session", "state", ("from", e.From), ("to", e.To), ("cause", e.Cause)));
                break;

            case WorkflowEvent.SessionEnded e:
                Record(new SessionEnd(_identity.SessionId, DateTimeOffset.UtcNow, e.FinalState));
                _logger.Info(Summarize(e.FinalState));
                _logger.CloseSession();
                break;
        }
    }

    private void LogBatchOutcome(BatchExecutionResult result, SelectionContext scope)
    {
        if (result.RolledBack)
        {
            _logger.Warn("Transaction rolled back; no elements were modified.", WarningCode.SessionRecordFailed);
            return;
        }

        var skippedIds = result.Skips.Select(s => s.Element.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var element in scope.ElementRefs.Where(e => !skippedIds.Contains(e.Id)))
            _logger.Info($"Updated {element.DisplayLabel}");
        foreach (var skip in result.Skips)
            _logger.Warn($"{skip.Element.DisplayLabel}: {skip.Message}", skip.Code);
    }

    private BatchResult Aggregate(BatchExecutionResult result, SelectionContext scope)
    {
        var skipped = new Dictionary<string, int>(StringComparer.Ordinal);
        var byCategory = new Dictionary<string, OutcomeCounts>(StringComparer.Ordinal);
        var skippedIds = result.Skips.Select(s => s.Element.Id).ToHashSet(StringComparer.Ordinal);

        foreach (var skip in result.Skips)
        {
            skipped[skip.Reason.ToString()] = skipped.GetValueOrDefault(skip.Reason.ToString()) + 1;
            Add(byCategory, skip.Element.CategoryName, warning: 1);
        }

        foreach (var element in scope.ElementRefs.Where(e => !skippedIds.Contains(e.Id)))
            Add(byCategory, element.CategoryName, success: 1);

        return new BatchResult(
            _identity.SessionId, DateTimeOffset.UtcNow, result.UpdatedCount, skipped, byCategory);
    }

    private string Summarize(SessionState finalState)
        => _lastBatch is { } b
            ? $"Session {finalState}: updated {b.UpdatedCount}, skipped {b.Skips.Count}."
            : $"Session ended in {finalState}";

    private void Record(MetricsRecord record)
    {
        try
        {
            _recorder.Record(record);
        }
        catch
        {
            _logger.Warn(
                ErrorWarningCatalog.Message(WarningCode.SessionRecordFailed),
                WarningCode.SessionRecordFailed);
        }

        _logger.Info(Describe(record));
    }

    private static string Describe(MetricsRecord record) => record switch
    {
        SessionStart r => $"Session started {r.SessionId}",
        SearchPerformed r => $"Search query='{r.QueryText}' matches={r.Matched.Count} [{string.Join(", ", r.Matched)}]",
        ParameterSelected r => $"Parameter selected {r.Name}",
        PhaseTiming r => $"Phase {r.Phase} {r.ElapsedMs}ms",
        BatchResult r =>
            $"Batch result updated={r.UpdatedCount} skipped={r.SkippedCounts.Values.Sum()} byCategory=[{string.Join("; ", r.CountsByCategory.Select(kv => $"{kv.Key} ok={kv.Value.Success} warn={kv.Value.Warning}"))}]",
        SessionEnd r => $"Session ended {r.FinalState}",
        _ => record.GetType().Name
    };

    private static void Add(Dictionary<string, OutcomeCounts> map, string category, int success = 0, int warning = 0, int error = 0)
    {
        map.TryGetValue(category, out var current);
        current ??= new OutcomeCounts(0, 0, 0);
        map[category] = new OutcomeCounts(
            current.Success + success, current.Warning + warning, current.Error + error);
    }
}
