using System.Text.Json;
using System.Text.Json.Serialization;
using BatchParamUpdate.Core;
using BatchParamUpdate.Domain.ErrorCatalog;
using BatchParamUpdate.Domain.Model;
using BatchParamUpdate.Domain.Ports;

namespace BatchParamUpdate.Adapters.Persistence;

public sealed class NdjsonSessionRecorder : ISessionRecorderPort
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly ILoggerPort _logger;

    public NdjsonSessionRecorder(string runId, string documentName, ILoggerPort logger)
    {
        _logger = logger;
        var dir = Path.Combine(Path.GetTempPath(), "juanManriqueHexagon", "TRACKER");
        Directory.CreateDirectory(dir);
        FilePath = Path.Combine(dir, $"revit-{runId}-{DocumentNameSanitizer.Sanitize(documentName)}.ndjson");
    }

    public string FilePath { get; }

    public void Record(MetricsRecord record)
    {
        try
        {
            File.AppendAllText(FilePath, JsonSerializer.Serialize(Shape(record), Json) + Environment.NewLine);
        }
        catch
        {
            _logger.Warn(
                ErrorWarningCatalog.Message(WarningCode.SessionRecordFailed),
                WarningCode.SessionRecordFailed);
        }
    }

    private static object Shape(MetricsRecord record) => record switch
    {
        SessionStart r => new { type = "session_start", r.SessionId, r.TimestampUtc },
        SearchPerformed r => new
        {
            type = "search_query",
            r.SessionId,
            r.TimestampUtc,
            r.QueryText,
            r.MatchedInInstanceSet,
            r.MatchedInTypeSet
        },
        ParameterSelected r => new { type = "parameter_selected", r.SessionId, r.TimestampUtc, r.Name, binding = r.Binding.ToString() },
        PhaseTiming r => new { type = "phase_timing", r.SessionId, r.TimestampUtc, r.Phase, r.ElapsedMs },
        BatchResult r => new
        {
            type = "batch_result",
            r.SessionId,
            r.TimestampUtc,
            path = r.Path.ToString(),
            r.UpdatedCount,
            r.SkippedCounts,
            r.CountsByCategory
        },
        SessionEnd r => new { type = "session_end", r.SessionId, r.TimestampUtc, finalState = r.FinalState.ToString() },
        _ => new { type = "unknown", record.SessionId, record.TimestampUtc }
    };
}
