using BatchParamUpdate.Core;
using BatchParamUpdate.Domain.Model;
using Xunit;

namespace BatchParamUpdate.Tests.Unit.Core;

public sealed class SessionTraceTests
{
    [Fact]
    public void Line_JoinsLayerSurfaceEventAndFacts()
    {
        var line = SessionTrace.Line(
            "ui",
            "run",
            "gate",
            ("enabled", false),
            ("session", SessionState.AwaitingReplacementValue),
            ("hasValue", true),
            ("subscribers", 0));

        Assert.Equal(
            "ui\trun\tgate\tenabled=false session=AwaitingReplacementValue hasValue=true subscribers=0",
            line);
    }

    [Fact]
    public void Line_QuotesFactsThatContainSpaces()
    {
        var line = SessionTrace.Line("cmd", "log", "open", ("path", @"C:\Users\Juan -- IP\log.txt"));
        Assert.Equal(@"cmd	log	open	path='C:\Users\Juan -- IP\log.txt'", line);
    }
}
