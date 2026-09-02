using BatchParamUpdate.Domain.Model;
using Xunit;

namespace BatchParamUpdate.Tests.Unit.Domain;

public sealed class ParameterKeyTests
{
    [Fact]
    public void ByName_HasNoBuiltInOrGuid()
    {
        var key = ParameterKey.ByName("Comments");
        Assert.Null(key.BuiltInId);
        Assert.Null(key.SharedGuid);
        Assert.Equal("Comments", key.Name);
    }

    [Fact]
    public void TwoArgCandidate_GetsNameKey()
    {
        var candidate = new ParameterCandidate("Mark", []);
        Assert.Equal(ParameterKey.ByName("Mark"), candidate.ResolvedKey);
    }

    [Fact]
    public void ResolvedKey_FallsBackToName_WhenKeyIsDefault()
    {
        var candidate = new ParameterCandidate("Mark", [], [], default);
        Assert.Equal("Mark", candidate.ResolvedKey.Name);
    }

    [Fact]
    public void ResolvedKey_KeepsBuiltInAndGuid_WhenProvided()
    {
        var guid = Guid.NewGuid();
        var candidate = new ParameterCandidate("Shared", [], [], new ParameterKey(42, guid, "Shared"));
        Assert.Equal(42, candidate.ResolvedKey.BuiltInId);
        Assert.Equal(guid, candidate.ResolvedKey.SharedGuid);
    }
}
