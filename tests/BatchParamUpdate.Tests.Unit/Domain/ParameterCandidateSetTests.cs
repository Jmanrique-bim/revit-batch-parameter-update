using BatchParamUpdate.Domain.Model;
using Xunit;

namespace BatchParamUpdate.Tests.Unit.Domain;

public sealed class ParameterCandidateSetTests
{
    [Fact]
    public void DeduplicatesNameOnce_MergingSourceRefsAndObservedValues()
    {
        var wall = new ElementRef("1", "Walls");
        var door = new ElementRef("2", "Doors");
        var set = new ParameterCandidateSet(
        [
            new("Comments", [wall], ["A"]),
            new("Comments", [door], ["B"]),
            new("Mark", [wall])
        ]);

        Assert.Equal(2, set.Candidates.Count);
        Assert.Equal(1, set.Candidates.Count(c => c.Name == "Comments"));
        var comments = set.Candidates.Single(c => c.Name == "Comments");
        Assert.Equal(2, comments.SourceElementRefs.Count);
        Assert.Equal(["A", "B"], comments.ObservedValues);
    }

    [Fact]
    public void Deduplication_IsCaseInsensitiveOnName()
    {
        var a = new ElementRef("1", "Walls");
        var b = new ElementRef("2", "Walls");
        var set = new ParameterCandidateSet(
        [
            new("Comments", [a]),
            new("comments", [b])
        ]);

        Assert.Single(set.Candidates);
        Assert.Equal(2, set.Candidates[0].SourceElementRefs.Count);
    }
}
