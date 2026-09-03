using BatchParamUpdate.Domain.Model;
using Xunit;

namespace BatchParamUpdate.Tests.Unit.Domain;

public sealed class ParameterSearchTests
{
    [Fact]
    public void Text_FiltersCandidatesByCaseInsensitiveSubstring()
    {
        var wall = new ElementRef("1", "Walls");
        var set = new ParameterCandidateSet(
        [
            new("Comments", [wall]),
            new("Mark", [wall])
        ]);
        var search = new ParameterSearch(set, "comMENT");

        Assert.Equal(["Comments"], search.Matches.Select(c => c.Name));
    }

    [Fact]
    public void EmptyText_ReturnsEveryCandidate()
    {
        var wall = new ElementRef("1", "Walls");
        var set = new ParameterCandidateSet([new("Mark", [wall]), new("Comments", [wall])]);

        var search = new ParameterSearch(set);

        Assert.Equal(2, search.Matches.Count);
    }

    [Fact]
    public void NoMatch_ReturnsEmpty()
    {
        var wall = new ElementRef("1", "Walls");
        var set = new ParameterCandidateSet([new("Mark", [wall])]);
        var search = new ParameterSearch(set, "zzz");

        Assert.Empty(search.Matches);
    }
}
