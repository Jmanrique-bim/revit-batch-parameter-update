using BatchParamUpdate.Application.Workflow;
using BatchParamUpdate.Domain.Model;
using BatchParamUpdate.Tests.Unit.Fakes;
using Xunit;

namespace BatchParamUpdate.Tests.Unit.Application;

/// <summary>End-to-end instance path through the coordinator with in-memory fakes.</summary>
public sealed class InstancePathUseCaseTests
{
    [Fact]
    public void PreSelection_ToSummary_UpdatesWritableAndSkipsMissing()
    {
        var writable = new ElementRef("ok", "Walls");
        var missing = new ElementRef("missing", "Doors");
        var h = new CoordinatorHarness();
        h.WithDiscovered("Comments");
        h.WithPreExisting(writable, missing);
        h.Write.SkipsByElementId["missing"] = SkipReason.ParameterMissing;

        var selection = h.Coordinator.EstablishSelection();
        Assert.IsType<SelectionResult.Established>(selection);
        Assert.Equal(SessionState.Discovering, h.Coordinator.Step);

        var chosen = h.Coordinator.ChooseParameter(h.Coordinator.Candidates.Candidates[0]);
        Assert.True(chosen);
        Assert.Equal(SessionState.AwaitingReplacementValue, h.Coordinator.Step);

        h.Coordinator.SetValue("batch-value");
        var result = h.Coordinator.Run();

        Assert.NotNull(result);
        Assert.False(result!.RolledBack);
        Assert.Equal(1, result.UpdatedCount);
        Assert.Single(result.Skips);
        Assert.Equal(SkipReason.ParameterMissing, result.Skips[0].Reason);

        h.Coordinator.Complete();
        Assert.Equal(SessionState.Completed, h.Coordinator.Step);
    }
}
