using BatchParamUpdate.Application.UseCases;
using BatchParamUpdate.Domain.Model;
using BatchParamUpdate.Tests.Unit.Fakes;
using Xunit;

namespace BatchParamUpdate.Tests.Unit.Application;

public sealed class EndToEndInstancePathTests
{
    [Fact]
    public void InstancePath_SelectionToSummary_UpdatesWritableAndSkipsMissing()
    {
        var writable = new ElementRef("ok", "Walls");
        var missing = new ElementRef("missing", "Doors");
        var selection = new FakeElementSelectionPort
        {
            PreExisting = new SelectionContext([writable, missing], SelectionOrigin.PreExisting)
        };
        var session = new Session();
        var scope = new EstablishSelectionUseCase(selection).Execute(session);
        Assert.True(scope.IsValid);
        Assert.Equal(SessionState.Discovering, session.State);

        var candidate = new ParameterCandidate("Comments", ParameterBinding.Instance, scope.ElementRefs);
        var discover = new DiscoverParametersUseCase(new FakeParameterDiscoveryPort
        {
            Instance = new InstanceParameterCandidateSet([candidate])
        });
        var (instanceSet, _) = discover.Discover(scope);
        Assert.Contains(instanceSet.Candidates, c => c.Name == "Comments");

        var operation = discover.Choose(candidate, scope, session);
        Assert.NotNull(operation);
        Assert.False(operation.RequiresWideBlastRadiusWarning);
        operation = operation.WithNewValue("batch-value");

        var write = new FakeParameterWritePort();
        write.SkipsByElementId["missing"] = SkipReason.ParameterMissing;
        var result = new RunBatchUpdateUseCase(write).Execute(session, operation, scope);

        Assert.NotNull(result);
        Assert.Equal(ParameterBinding.Instance, result.Path);
        Assert.Equal(1, result.InstanceOutcome!.UpdatedCount);
        Assert.Single(result.InstanceOutcome.Skips);
        Assert.Equal(SkipReason.ParameterMissing, result.InstanceOutcome.Skips[0].Reason);
        Assert.Equal(SessionState.Completed, session.State);
    }
}
