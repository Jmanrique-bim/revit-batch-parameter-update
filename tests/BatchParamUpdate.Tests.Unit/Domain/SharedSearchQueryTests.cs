using BatchParamUpdate.Domain.Model;
using Xunit;

namespace BatchParamUpdate.Tests.Unit.Domain;

public sealed class SharedSearchQueryTests
{
    [Fact]
    public void Text_FiltersBothSetsByCaseInsensitiveSubstring()
    {
        var wall = new ElementRef("1", "Walls");
        var instance = new InstanceParameterCandidateSet(
        [
            new("Comments", ParameterBinding.Instance, [wall]),
            new("Mark", ParameterBinding.Instance, [wall])
        ]);
        var type = new TypeParameterCandidateSet(
        [
            new("Type Comments", ParameterBinding.Type, [wall]),
            new("Type Mark", ParameterBinding.Type, [wall])
        ]);
        var query = new SharedSearchQuery(instance, type, "comMENT");

        Assert.Equal(["Comments"], query.MatchesInstanceSet.Select(c => c.Name));
        Assert.Equal(["Type Comments"], query.MatchesTypeSet.Select(c => c.Name));
    }

    [Fact]
    public void EmptyMatches_AreIndependentPerSet()
    {
        var wall = new ElementRef("1", "Walls");
        var instance = new InstanceParameterCandidateSet(
            [new("Mark", ParameterBinding.Instance, [wall])]);
        var type = new TypeParameterCandidateSet(
            [new("Type Comments", ParameterBinding.Type, [wall])]);
        var query = new SharedSearchQuery(instance, type, "zzz");

        Assert.Empty(query.MatchesInstanceSet);
        Assert.Empty(query.MatchesTypeSet);
    }
}
