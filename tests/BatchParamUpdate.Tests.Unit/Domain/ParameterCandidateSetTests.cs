using BatchParamUpdate.Domain.Model;
using Xunit;

namespace BatchParamUpdate.Tests.Unit.Domain;

public sealed class ParameterCandidateSetTests
{
    [Fact]
    public void InstanceSet_DeduplicatesNameOncePerBinding()
    {
        var wall = new ElementRef("1", "Walls");
        var door = new ElementRef("2", "Doors");
        var set = new InstanceParameterCandidateSet(
        [
            new("Comments", ParameterBinding.Instance, [wall], ["A"]),
            new("Comments", ParameterBinding.Instance, [door], ["B"]),
            new("Mark", ParameterBinding.Instance, [wall]),
            new("Type Comments", ParameterBinding.Type, [wall])
        ]);

        Assert.Equal(2, set.Candidates.Count);
        Assert.All(set.Candidates, c => Assert.Equal(ParameterBinding.Instance, c.Binding));
        Assert.Equal(1, set.Candidates.Count(c => c.Name == "Comments"));
        var comments = set.Candidates.Single(c => c.Name == "Comments");
        Assert.Equal(2, comments.SourceElementRefs.Count);
        Assert.Equal(["A", "B"], comments.ObservedValues);
    }

    [Fact]
    public void TypeSet_DeduplicatesNameOncePerBinding()
    {
        var a = new ElementRef("1", "Walls");
        var b = new ElementRef("2", "Walls");
        var set = new TypeParameterCandidateSet(
        [
            new("Type Comments", ParameterBinding.Type, [a]),
            new("Type Comments", ParameterBinding.Type, [b]),
            new("Comments", ParameterBinding.Instance, [a])
        ]);

        Assert.Single(set.Candidates);
        Assert.Equal("Type Comments", set.Candidates[0].Name);
        Assert.Equal(2, set.Candidates[0].SourceElementRefs.Count);
    }
}
