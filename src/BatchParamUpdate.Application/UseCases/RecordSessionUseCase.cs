using BatchParamUpdate.Core;
using BatchParamUpdate.Domain.ErrorCatalog;
using BatchParamUpdate.Domain.Model;
using BatchParamUpdate.Domain.Ports;

namespace BatchParamUpdate.Application.UseCases;

public sealed class RecordSessionUseCase
{
    private readonly ISessionRecorderPort _recorder;
    private readonly ILoggerPort _logger;
    private readonly SessionRecord _identity;
    private BatchExecutionResult? _lastBatch;
    private bool _ended;
    private string? _lastGate;

    public RecordSessionUseCase(ISessionRecorderPort recorder, ILoggerPort logger, SessionRecord identity)
    {
        _recorder = recorder;
        _logger = logger;
        _identity = identity;
    }

    public string SessionId => _identity.SessionId;

    public bool HasBatch => _lastBatch is not null;

    public void Start()
    {
        SafeRecord(new SessionStart(_identity.SessionId, DateTimeOffset.UtcNow));
    }

    public void RecordSearch(
        string queryText,
        IReadOnlyList<string> matchedInInstanceSet,
        IReadOnlyList<string> matchedInTypeSet)
        => SafeRecord(new SearchPerformed(
            _identity.SessionId,
            DateTimeOffset.UtcNow,
            queryText,
            matchedInInstanceSet,
            matchedInTypeSet));

    public void RecordParameterSelected(ParameterCandidate candidate)
        => SafeRecord(new ParameterSelected(
            _identity.SessionId,
            DateTimeOffset.UtcNow,
            candidate.Name,
            candidate.Binding));

    public void RecordPhaseTiming(string phase, long elapsedMs)
        => SafeRecord(new PhaseTiming(_identity.SessionId, DateTimeOffset.UtcNow, phase, elapsedMs));

    public void Trace(string message) => _logger.Info(message);

    public void Trace(string layer, string surface, string evt, params (string Key, object? Value)[] facts)
        => _logger.Info(SessionTrace.Line(layer, surface, evt, facts));

    public void TraceState(SessionState from, Session session, string cause)
    {
        if (from == session.State)
            return;
        Trace("model", "session", "state", ("from", from), ("to", session.State), ("cause", cause));
    }

    // ponytail: identical CanExecute polls would flood; log a gate line only when facts change.
    public void TraceGate(params (string Key, object? Value)[] facts)
    {
        var line = SessionTrace.Line("ui", "run", "gate", facts);
        if (line == _lastGate)
            return;
        _lastGate = line;
        _logger.Info(line);
    }

    public void RecordBatch(BatchExecutionResult result, SelectionContext scope)
    {
        _lastBatch = result;
        SafeRecord(Aggregate(result, scope));
    }

    public void End(Session session)
    {
        if (_ended)
            return;

        _ended = true;
        SafeRecord(new SessionEnd(_identity.SessionId, DateTimeOffset.UtcNow, session.State));
        _logger.Info(Summarize(session));
        _logger.CloseSession();
    }

    private BatchResult Aggregate(BatchExecutionResult result, SelectionContext scope)
    {
        var skipped = new Dictionary<string, int>(StringComparer.Ordinal);
        var byCategory = new Dictionary<string, OutcomeCounts>(StringComparer.Ordinal);

        if (result.InstanceOutcome is { } instance)
        {
            var skippedIds = instance.Skips.Select(s => s.Element.Id).ToHashSet(StringComparer.Ordinal);
            foreach (var skip in instance.Skips)
            {
                skipped[skip.Reason.ToString()] = skipped.GetValueOrDefault(skip.Reason.ToString()) + 1;
                Add(byCategory, skip.Element.CategoryName, warning: 1);
            }

            foreach (var element in scope.ElementRefs.Where(e => !skippedIds.Contains(e.Id)))
                Add(byCategory, element.CategoryName, success: 1);

            return new BatchResult(
                _identity.SessionId,
                DateTimeOffset.UtcNow,
                result.Path,
                instance.UpdatedCount,
                skipped,
                byCategory);
        }

        var type = result.TypeOutcome!;
        foreach (var resolved in type.AffectedTypes)
        {
            foreach (var element in resolved.SourceElementRefs)
                Add(byCategory, element.CategoryName, success: 1);
        }

        // ponytail: Type-path model-wide extras are not per-element; remainder is one Model bucket.
        var categorized = byCategory.Values.Sum(c => c.Success);
        if (type.TotalElementsUpdated > categorized)
            Add(byCategory, "Model", success: type.TotalElementsUpdated - categorized);

        return new BatchResult(
            _identity.SessionId,
            DateTimeOffset.UtcNow,
            result.Path,
            type.TotalElementsUpdated,
            skipped,
            byCategory);
    }

    private string Summarize(Session session)
    {
        if (_lastBatch?.InstanceOutcome is { } instance)
            return $"Session {session.State}: updated {instance.UpdatedCount}, skipped {instance.Skips.Count}.";

        if (_lastBatch?.TypeOutcome is { } type)
            return $"Session {session.State}: updated {type.TotalElementsUpdated} across {type.AffectedTypes.Count} type(s).";

        return $"Session ended in {session.State}";
    }

    private void SafeRecord(MetricsRecord record)
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
        SearchPerformed r =>
            $"Search query='{r.QueryText}' instance={r.MatchedInInstanceSet.Count} [{string.Join(", ", r.MatchedInInstanceSet)}] type={r.MatchedInTypeSet.Count} [{string.Join(", ", r.MatchedInTypeSet)}]",
        ParameterSelected r => $"Parameter selected {r.Name} ({r.Binding})",
        PhaseTiming r => $"Phase {r.Phase} {r.ElapsedMs}ms",
        BatchResult r =>
            $"Batch result path={r.Path} updated={r.UpdatedCount} skipped={r.SkippedCounts.Values.Sum()} byCategory=[{string.Join("; ", r.CountsByCategory.Select(kv => $"{kv.Key} ok={kv.Value.Success} warn={kv.Value.Warning}"))}]",
        SessionEnd r => $"Session ended {r.FinalState}",
        _ => record.GetType().Name
    };

    private static void Add(Dictionary<string, OutcomeCounts> map, string category, int success = 0, int warning = 0, int error = 0)
    {
        map.TryGetValue(category, out var current);
        current ??= new OutcomeCounts(0, 0, 0);
        map[category] = new OutcomeCounts(
            current.Success + success,
            current.Warning + warning,
            current.Error + error);
    }
}
