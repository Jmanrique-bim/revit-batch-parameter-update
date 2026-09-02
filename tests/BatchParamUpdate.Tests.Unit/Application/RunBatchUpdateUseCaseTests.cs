using BatchParamUpdate.Application.UseCases;
using BatchParamUpdate.Domain.ErrorCatalog;
using BatchParamUpdate.Domain.Model;
using BatchParamUpdate.Tests.Unit.Fakes;
using Xunit;

namespace BatchParamUpdate.Tests.Unit.Application;

public sealed class RunBatchUpdateUseCaseTests
{
    private static readonly SelectionContext Scope = new(
        [
            new ElementRef("missing", "Walls"),
            new ElementRef("ro", "Walls"),
            new ElementRef("notext", "Doors"),
            new ElementRef("owned", "Walls"),
            new ElementRef("group", "Walls")
        ],
        SelectionOrigin.PreExisting);

    private static readonly ParameterCandidate InstanceParam = new("Comments", Scope.ElementRefs);

    private static readonly IProgress<BatchProgress> NoProgress = new Progress<BatchProgress>();

    [Fact]
    public void Execute_RejectsBlankNewValue_WithEmptyValueError()
    {
        var write = new FakeParameterWritePort();
        var useCase = new RunBatchUpdateUseCase(write);
        var session = AwaitingSession();
        var operation = new ReplacementOperation(InstanceParam, "   ", Scope);

        var result = useCase.Execute(session, operation, Scope, NoProgress);

        Assert.Null(result);
        Assert.Equal(ErrorCode.EmptyValue, useCase.Error);
        Assert.Equal(0, write.ExecuteCalls);
        Assert.Equal(SessionState.AwaitingReplacementValue, session.State);
    }

    [Fact]
    public void Execute_ProducesSkipForEachConfiguredReason()
    {
        var write = new FakeParameterWritePort();
        write.SkipsByElementId["missing"] = SkipReason.ParameterMissing;
        write.SkipsByElementId["ro"] = SkipReason.ParameterReadOnly;
        write.SkipsByElementId["notext"] = SkipReason.ParameterNotText;
        write.SkipsByElementId["owned"] = SkipReason.WorksharingOwnedByOther;
        write.SkipsByElementId["group"] = SkipReason.ModelGroupMember;
        var useCase = new RunBatchUpdateUseCase(write);
        var session = AwaitingSession();
        var operation = new ReplacementOperation(InstanceParam, "new", Scope);

        var result = useCase.Execute(session, operation, Scope, NoProgress);

        Assert.NotNull(result);
        Assert.False(result.RolledBack);
        Assert.Equal(0, result.UpdatedCount);
        Assert.Equal(5, result.Skips.Count);
        Assert.Contains(result.Skips, s => s.Reason == SkipReason.ParameterMissing);
        Assert.Contains(result.Skips, s => s.Reason == SkipReason.ParameterReadOnly);
        Assert.Contains(result.Skips, s => s.Reason == SkipReason.ParameterNotText);
        Assert.Contains(result.Skips, s => s.Reason == SkipReason.WorksharingOwnedByOther);
        Assert.Contains(result.Skips, s => s.Reason == SkipReason.ModelGroupMember);
        Assert.Equal(SessionState.AwaitingReplacementValue, session.State);
    }

    [Fact]
    public void Execute_TwiceInSameSession_StaysAwaitingReplacementValue()
    {
        var write = new FakeParameterWritePort();
        var useCase = new RunBatchUpdateUseCase(write);
        var session = AwaitingSession();
        var operation = new ReplacementOperation(InstanceParam, "new", Scope);

        useCase.Execute(session, operation, Scope, NoProgress);
        var second = useCase.Execute(session, operation, Scope, NoProgress);

        Assert.NotNull(second);
        Assert.Equal(2, write.ExecuteCalls);
        Assert.Equal(SessionState.AwaitingReplacementValue, session.State);
    }

    [Fact]
    public void Execute_WhenTransactionReverts_ReportsRolledBackWithoutSuccessCount()
    {
        var write = new FakeParameterWritePort { Revert = true };
        var useCase = new RunBatchUpdateUseCase(write);
        var session = AwaitingSession();
        var operation = new ReplacementOperation(InstanceParam, "new", Scope);

        var result = useCase.Execute(session, operation, Scope, NoProgress);

        Assert.NotNull(result);
        Assert.True(result.RolledBack);
        Assert.Equal(0, result.UpdatedCount);
    }

    [Fact]
    public void Execute_WhenGloballyBlocked_ProducesNoResultAndBlocksSession()
    {
        var write = new FakeParameterWritePort { BlockGlobally = true };
        var useCase = new RunBatchUpdateUseCase(write);
        var session = AwaitingSession();
        var operation = new ReplacementOperation(InstanceParam, "new", Scope);

        var result = useCase.Execute(session, operation, Scope, NoProgress);

        Assert.Null(result);
        Assert.Equal(ErrorCode.DocumentNotModifiable, useCase.Error);
        Assert.Equal(SessionState.Blocked, session.State);
        Assert.Equal(1, write.ExecuteCalls);
    }

    private static Session AwaitingSession()
    {
        var session = new Session();
        session.TransitionTo(SessionState.Discovering);
        session.TransitionTo(SessionState.AwaitingReplacementValue);
        return session;
    }
}
