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

    private static readonly ParameterCandidate InstanceParam = new(
        "Comments",
        ParameterBinding.Instance,
        Scope.ElementRefs);

    [Fact]
    public void Execute_RejectsBlankNewValue_WithEmptyValueError()
    {
        var write = new FakeParameterWritePort();
        var useCase = new RunBatchUpdateUseCase(write);
        var session = AwaitingSession();
        var operation = new ReplacementOperation(InstanceParam, "   ", new InstanceScope(Scope));

        var result = useCase.Execute(session, operation, Scope);

        Assert.Null(result);
        Assert.Equal(ErrorCode.EmptyValue, useCase.Error);
        Assert.Equal(0, write.InstanceUpdateCalls);
        Assert.Equal(SessionState.AwaitingReplacementValue, session.State);
    }

    [Fact]
    public void ExecuteInstanceUpdate_ProducesSkipForEachConfiguredReason()
    {
        var write = new FakeParameterWritePort();
        write.SkipsByElementId["missing"] = SkipReason.ParameterMissing;
        write.SkipsByElementId["ro"] = SkipReason.ParameterReadOnly;
        write.SkipsByElementId["notext"] = SkipReason.ParameterNotText;
        write.SkipsByElementId["owned"] = SkipReason.WorksharingOwnedByOther;
        write.SkipsByElementId["group"] = SkipReason.ModelGroupMember;
        var useCase = new RunBatchUpdateUseCase(write);
        var session = AwaitingSession();
        var operation = new ReplacementOperation(InstanceParam, "new", new InstanceScope(Scope));

        var result = useCase.Execute(session, operation, Scope);

        Assert.NotNull(result);
        Assert.Equal(ParameterBinding.Instance, result.Path);
        Assert.Equal(0, result.InstanceOutcome!.UpdatedCount);
        Assert.Equal(5, result.InstanceOutcome.Skips.Count);
        Assert.Contains(result.InstanceOutcome.Skips, s => s.Reason == SkipReason.ParameterMissing);
        Assert.Contains(result.InstanceOutcome.Skips, s => s.Reason == SkipReason.ParameterReadOnly);
        Assert.Contains(result.InstanceOutcome.Skips, s => s.Reason == SkipReason.ParameterNotText);
        Assert.Contains(result.InstanceOutcome.Skips, s => s.Reason == SkipReason.WorksharingOwnedByOther);
        Assert.Contains(result.InstanceOutcome.Skips, s => s.Reason == SkipReason.ModelGroupMember);
        Assert.Equal(SessionState.Completed, session.State);
    }

    [Fact]
    public void ExecuteTypeUpdate_ReturnsAffectedTypesAndModelWideCount()
    {
        var type = new ResolvedType("t1", "Basic Wall", [new ElementRef("1", "Walls")]);
        var write = new FakeParameterWritePort
        {
            TypeUpdateResult = BatchExecutionResult.ForType([type], totalElementsUpdated: 12)
        };
        var candidate = new ParameterCandidate("Type Comments", ParameterBinding.Type, [new ElementRef("1", "Walls")]);
        var useCase = new RunBatchUpdateUseCase(write);
        var session = AwaitingSession();
        var operation = new ReplacementOperation(candidate, "new", new TypeScope([type]));

        var result = useCase.Execute(session, operation, Scope);

        Assert.NotNull(result);
        Assert.Equal(ParameterBinding.Type, result.Path);
        Assert.Equal(12, result.TypeOutcome!.TotalElementsUpdated);
        Assert.Single(result.TypeOutcome.AffectedTypes);
        Assert.Equal("Basic Wall", result.TypeOutcome.AffectedTypes[0].Name);
        Assert.Equal(1, write.TypeUpdateCalls);
    }

    [Fact]
    public void Execute_WhenGloballyBlocked_ProducesNoResultAndTouchesNothing()
    {
        var write = new FakeParameterWritePort { BlockGlobally = true };
        var useCase = new RunBatchUpdateUseCase(write);
        var session = AwaitingSession();
        var operation = new ReplacementOperation(InstanceParam, "new", new InstanceScope(Scope));

        var result = useCase.Execute(session, operation, Scope);

        Assert.Null(result);
        Assert.Equal(ErrorCode.DocumentNotModifiable, useCase.Error);
        Assert.Equal(SessionState.Blocked, session.State);
        Assert.Equal(1, write.InstanceUpdateCalls);
    }

    private static Session AwaitingSession()
    {
        var session = new Session();
        session.TransitionTo(SessionState.Discovering);
        session.TransitionTo(SessionState.AwaitingReplacementValue);
        return session;
    }
}
