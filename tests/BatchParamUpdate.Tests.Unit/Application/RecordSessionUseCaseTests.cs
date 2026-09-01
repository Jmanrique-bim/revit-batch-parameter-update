using BatchParamUpdate.Application.UseCases;
using BatchParamUpdate.Domain.ErrorCatalog;
using BatchParamUpdate.Domain.Model;
using BatchParamUpdate.Tests.Unit.Fakes;
using Xunit;

namespace BatchParamUpdate.Tests.Unit.Application;

public sealed class RecordSessionUseCaseTests
{
    private static readonly SessionRecord Identity = new("abcd1234", "TestDoc", DateTimeOffset.UtcNow);

    [Fact]
    public void Start_WritesSessionStart()
    {
        var recorder = new FakeSessionRecorderPort();
        var useCase = new RecordSessionUseCase(recorder, new FakeLoggerPort(), Identity);

        useCase.Start();

        Assert.Single(recorder.Records);
        Assert.IsType<SessionStart>(recorder.Records[0]);
        Assert.Equal(Identity.SessionId, recorder.Records[0].SessionId);
    }

    [Fact]
    public void RecordBatch_AggregatesSkippedCountsAndCountsByCategory()
    {
        var recorder = new FakeSessionRecorderPort();
        var useCase = new RecordSessionUseCase(recorder, new FakeLoggerPort(), Identity);
        var walls = new ElementRef("1", "Walls");
        var doors = new ElementRef("2", "Doors");
        var scope = new SelectionContext([walls, doors], SelectionOrigin.PreExisting);
        var result = BatchExecutionResult.ForInstance(
            updatedCount: 1,
            [ElementSkip.Create(doors, SkipReason.ParameterMissing)]);

        useCase.RecordBatch(result, scope);

        var batch = Assert.IsType<BatchResult>(Assert.Single(recorder.Records));
        Assert.Equal(ParameterBinding.Instance, batch.Path);
        Assert.Equal(1, batch.UpdatedCount);
        Assert.Equal(1, batch.SkippedCounts[nameof(SkipReason.ParameterMissing)]);
        Assert.Equal(1, batch.CountsByCategory["Walls"].Success);
        Assert.Equal(0, batch.CountsByCategory["Walls"].Warning);
        Assert.Equal(1, batch.CountsByCategory["Doors"].Warning);
        Assert.Equal(0, batch.CountsByCategory["Doors"].Success);
    }

    [Theory]
    [InlineData(SessionState.Completed)]
    [InlineData(SessionState.Blocked)]
    [InlineData(SessionState.Cancelled)]
    public void End_EmitsSessionEndWithFinalState(SessionState final)
    {
        var recorder = new FakeSessionRecorderPort();
        var logger = new FakeLoggerPort();
        var useCase = new RecordSessionUseCase(recorder, logger, Identity);
        var session = SessionIn(final);

        useCase.End(session);

        var end = Assert.IsType<SessionEnd>(Assert.Single(recorder.Records));
        Assert.Equal(final, end.FinalState);
        Assert.True(logger.Closed);
        Assert.Contains(logger.Lines, line => line.StartsWith("INFO ", StringComparison.Ordinal) && line.Contains(final.ToString()));
    }

    [Fact]
    public void RecorderFailure_DoesNotThrow_AndLogsSessionRecordFailed()
    {
        var recorder = new FakeSessionRecorderPort { ThrowOnRecord = new IOException("disk full") };
        var logger = new FakeLoggerPort();
        var useCase = new RecordSessionUseCase(recorder, logger, Identity);

        var thrown = Record.Exception(() => useCase.Start());

        Assert.Null(thrown);
        Assert.Empty(recorder.Records);
        Assert.Contains(logger.Lines, line =>
            line.Contains(nameof(WarningCode.SessionRecordFailed), StringComparison.Ordinal)
            && line.Contains(ErrorWarningCatalog.Message(WarningCode.SessionRecordFailed)));
    }

    private static Session SessionIn(SessionState final)
    {
        var session = new Session();
        if (final == SessionState.Cancelled)
        {
            session.TransitionTo(SessionState.Cancelled);
            return session;
        }

        session.TransitionTo(SessionState.Discovering);
        session.TransitionTo(SessionState.AwaitingReplacementValue);
        session.TransitionTo(SessionState.Executing);
        session.TransitionTo(final);
        return session;
    }
}
