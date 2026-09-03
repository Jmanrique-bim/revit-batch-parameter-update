using BatchParamUpdate.Application.Observability;
using BatchParamUpdate.Domain.ErrorCatalog;
using BatchParamUpdate.Domain.Model;
using BatchParamUpdate.Tests.Unit.Fakes;
using Xunit;
using static BatchParamUpdate.Application.Observability.WorkflowEvent;

namespace BatchParamUpdate.Tests.Unit.Application;

public sealed class SessionTraceListenerTests
{
    private static readonly SessionRecord Identity = new("abcd1234", "TestDoc", DateTimeOffset.UtcNow);

    private static (SessionTraceListener listener, FakeSessionRecorderPort recorder, FakeLoggerPort logger) NewListener()
    {
        var recorder = new FakeSessionRecorderPort();
        var logger = new FakeLoggerPort();
        return (new SessionTraceListener(recorder, logger, Identity), recorder, logger);
    }

    [Fact]
    public void SessionStarted_WritesSessionStartRecordAndLine()
    {
        var (listener, recorder, logger) = NewListener();

        listener.On(new SessionStarted(Identity.SessionId));

        Assert.IsType<SessionStart>(Assert.Single(recorder.Records));
        Assert.Contains(logger.Lines, l => l.Contains("Session started", StringComparison.Ordinal) && l.Contains(Identity.SessionId));
    }

    [Fact]
    public void SearchPerformed_RecordsQueryAndMatches()
    {
        var (listener, recorder, logger) = NewListener();

        listener.On(new SearchRan("mark", ["Mark", "Remark"]));

        var record = Assert.IsType<BatchParamUpdate.Domain.Model.SearchPerformed>(Assert.Single(recorder.Records));
        Assert.Equal("mark", record.QueryText);
        Assert.Equal(2, record.Matched.Count);
        Assert.Contains(logger.Lines, l => l.Contains("Search", StringComparison.Ordinal) && l.Contains("mark"));
    }

    [Fact]
    public void BatchFinished_AggregatesSkippedCountsAndCountsByCategory()
    {
        var (listener, recorder, _) = NewListener();
        var walls = new ElementRef("1", "Walls");
        var doors = new ElementRef("2", "Doors");
        var scope = new SelectionContext([walls, doors], SelectionOrigin.PreExisting);
        var result = BatchExecutionResult.Committed(1, [ElementSkip.Create(doors, SkipReason.ParameterMissing)]);

        listener.On(new BatchFinished(result, scope, ElapsedMs: 5));

        Assert.Contains(recorder.Records, r => r is PhaseTiming { Phase: "Execution" });
        var batch = Assert.IsType<BatchResult>(recorder.Records.Single(r => r is BatchResult));
        Assert.Equal(1, batch.UpdatedCount);
        Assert.Equal(1, batch.SkippedCounts[nameof(SkipReason.ParameterMissing)]);
        Assert.Equal(1, batch.CountsByCategory["Walls"].Success);
        Assert.Equal(1, batch.CountsByCategory["Doors"].Warning);
    }

    [Fact]
    public void BatchFinished_WhenRolledBack_DoesNotCountSuccesses()
    {
        var (listener, recorder, logger) = NewListener();
        var walls = new ElementRef("1", "Walls");
        var doors = new ElementRef("2", "Doors");
        var scope = new SelectionContext([walls, doors], SelectionOrigin.PreExisting);
        var result = BatchExecutionResult.Reverted([ElementSkip.Create(doors, SkipReason.ParameterMissing)]);

        listener.On(new BatchFinished(result, scope, ElapsedMs: 5));

        var batch = Assert.IsType<BatchResult>(recorder.Records.Single(r => r is BatchResult));
        Assert.Equal(0, batch.UpdatedCount);
        Assert.Equal(1, batch.SkippedCounts[nameof(SkipReason.ParameterMissing)]);
        Assert.Equal(1, batch.CountsByCategory["Doors"].Warning);
        Assert.False(batch.CountsByCategory.ContainsKey("Walls"));
        Assert.DoesNotContain(
            logger.Lines,
            l => l.Contains(nameof(WarningCode.SessionRecordFailed), StringComparison.Ordinal));
    }

    [Fact]
    public void StateChanged_WritesLayeredLine()
    {
        var (listener, _, logger) = NewListener();

        listener.On(new StateChanged(SessionState.Started, SessionState.Discovering, "preselect"));

        Assert.Contains(
            logger.Lines,
            l => l == "INFO model\tsession\tstate\tfrom=Started to=Discovering cause=preselect");
    }

    [Fact]
    public void SessionEnded_EmitsSessionEndAndClosesLog()
    {
        var (listener, recorder, logger) = NewListener();

        listener.On(new SessionEnded(SessionState.Cancelled));

        var end = Assert.IsType<SessionEnd>(recorder.Records.Single(r => r is SessionEnd));
        Assert.Equal(SessionState.Cancelled, end.FinalState);
        Assert.True(logger.Closed);
    }

    [Fact]
    public void SessionEnded_WritesCloseDiagnosisLine()
    {
        var (listener, _, logger) = NewListener();

        listener.On(new SessionEnded(SessionState.Cancelled)
        {
            Why = "can-run-never-clicked",
            CanRun = true,
            HasTarget = true,
            Parameter = "Mark",
            HasValue = true,
            Value = "test 1",
            Scope = 26,
            Origin = SelectionOrigin.ManualPick,
            Candidates = 12
        });

        Assert.Contains(
            logger.Lines,
            l => l.StartsWith("INFO ui\trun\tclose\t", StringComparison.Ordinal)
                 && l.Contains("why=can-run-never-clicked", StringComparison.Ordinal)
                 && l.Contains("canRun=true", StringComparison.Ordinal)
                 && l.Contains("param=Mark", StringComparison.Ordinal)
                 && l.Contains("hasValue=true", StringComparison.Ordinal)
                 && l.Contains("origin=ManualPick", StringComparison.Ordinal));
        Assert.Contains(logger.Lines, l => l.Contains("Session ended in Cancelled: can-run-never-clicked"));
    }

    [Fact]
    public void RecorderFailure_IsSwallowed_AndLogsSessionRecordFailed()
    {
        var recorder = new FakeSessionRecorderPort { ThrowOnRecord = new IOException("disk full") };
        var logger = new FakeLoggerPort();
        var listener = new SessionTraceListener(recorder, logger, Identity);

        var thrown = Record.Exception(() => listener.On(new SessionStarted(Identity.SessionId)));

        Assert.Null(thrown);
        Assert.Empty(recorder.Records);
        Assert.Contains(logger.Lines, l =>
            l.Contains(nameof(WarningCode.SessionRecordFailed), StringComparison.Ordinal)
            && l.Contains(ErrorWarningCatalog.Message(WarningCode.SessionRecordFailed)));
    }
}
