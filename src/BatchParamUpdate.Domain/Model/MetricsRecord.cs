namespace BatchParamUpdate.Domain.Model;

public abstract record MetricsRecord(string SessionId, DateTimeOffset TimestampUtc);

public sealed record SessionStart(string SessionId, DateTimeOffset TimestampUtc)
    : MetricsRecord(SessionId, TimestampUtc);

public sealed record SearchPerformed(
    string SessionId,
    DateTimeOffset TimestampUtc,
    string QueryText,
    IReadOnlyList<string> MatchedInInstanceSet,
    IReadOnlyList<string> MatchedInTypeSet) : MetricsRecord(SessionId, TimestampUtc);

public sealed record ParameterSelected(
    string SessionId,
    DateTimeOffset TimestampUtc,
    string Name,
    ParameterBinding Binding) : MetricsRecord(SessionId, TimestampUtc);

public sealed record PhaseTiming(
    string SessionId,
    DateTimeOffset TimestampUtc,
    string Phase,
    long ElapsedMs) : MetricsRecord(SessionId, TimestampUtc);

public sealed record OutcomeCounts(int Success, int Warning, int Error);

public sealed record BatchResult(
    string SessionId,
    DateTimeOffset TimestampUtc,
    ParameterBinding Path,
    int UpdatedCount,
    IReadOnlyDictionary<string, int> SkippedCounts,
    IReadOnlyDictionary<string, OutcomeCounts> CountsByCategory) : MetricsRecord(SessionId, TimestampUtc);

public sealed record SessionEnd(
    string SessionId,
    DateTimeOffset TimestampUtc,
    SessionState FinalState) : MetricsRecord(SessionId, TimestampUtc);
