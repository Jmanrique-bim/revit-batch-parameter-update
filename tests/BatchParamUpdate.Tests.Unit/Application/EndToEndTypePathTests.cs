using BatchParamUpdate.Application.UseCases;
using BatchParamUpdate.Domain.Model;
using BatchParamUpdate.Tests.Unit.Fakes;
using Xunit;

namespace BatchParamUpdate.Tests.Unit.Application;

public sealed class EndToEndTypePathTests
{
    [Fact]
    public void TypePath_ShowsNonBlockingWarning_AndReportsModelWideCount()
    {
        var element = new ElementRef("1", "Walls");
        var scope = new SelectionContext([element], SelectionOrigin.PreExisting);
        var session = new Session();
        session.TransitionTo(SessionState.Discovering);

        var candidate = new ParameterCandidate("Type Comments", ParameterBinding.Type, [element]);
        var discover = new DiscoverParametersUseCase(new FakeParameterDiscoveryPort
        {
            Type = new TypeParameterCandidateSet([candidate])
        });
        discover.Discover(scope);
        var operation = discover.Choose(candidate, scope, session);

        Assert.NotNull(operation);
        Assert.True(operation.RequiresWideBlastRadiusWarning);
        Assert.Equal(SessionState.AwaitingReplacementValue, session.State);

        var type = new ResolvedType("t1", "Basic Wall", [element]);
        var write = new FakeParameterWritePort
        {
            TypeUpdateResult = BatchExecutionResult.ForType([type], totalElementsUpdated: 8)
        };
        var result = new RunBatchUpdateUseCase(write).Execute(session, operation.WithNewValue("type-value"), scope);

        Assert.NotNull(result);
        Assert.Equal(ParameterBinding.Type, result.Path);
        Assert.Equal(8, result.TypeOutcome!.TotalElementsUpdated);
        Assert.Equal("Basic Wall", result.TypeOutcome.AffectedTypes[0].Name);
        Assert.Equal(SessionState.AwaitingReplacementValue, session.State);
    }
}
