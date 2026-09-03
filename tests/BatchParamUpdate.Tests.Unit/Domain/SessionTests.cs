using BatchParamUpdate.Domain.Model;
using Xunit;

namespace BatchParamUpdate.Tests.Unit.Domain;

public sealed class SessionTests
{
    [Fact]
    public void HappyPath_StartedToCompleted()
    {
        var session = new Session();
        session.TransitionTo(SessionState.Discovering);
        session.TransitionTo(SessionState.AwaitingReplacementValue);
        session.TransitionTo(SessionState.Executing);
        session.TransitionTo(SessionState.AwaitingReplacementValue);
        session.TransitionTo(SessionState.Completed);
        Assert.Equal(SessionState.Completed, session.State);
    }

    [Fact]
    public void Executing_CanReturnToAwaitingReplacementValue()
    {
        var session = AdvanceTo(SessionState.Executing);
        session.TransitionTo(SessionState.AwaitingReplacementValue);
        Assert.Equal(SessionState.AwaitingReplacementValue, session.State);
    }

    [Fact]
    public void Executing_CanBlock()
    {
        var session = new Session();
        session.TransitionTo(SessionState.Discovering);
        session.TransitionTo(SessionState.AwaitingReplacementValue);
        session.TransitionTo(SessionState.Executing);
        session.TransitionTo(SessionState.Blocked);
        Assert.Equal(SessionState.Blocked, session.State);
    }

    [Theory]
    [InlineData(SessionState.Started)]
    [InlineData(SessionState.Discovering)]
    [InlineData(SessionState.AwaitingReplacementValue)]
    [InlineData(SessionState.Executing)]
    public void NonTerminal_CanCancel(SessionState from)
    {
        var session = AdvanceTo(from);
        session.TransitionTo(SessionState.Cancelled);
        Assert.Equal(SessionState.Cancelled, session.State);
    }

    [Fact]
    public void AwaitingReplacementValue_CanReturnToDiscovering()
    {
        var session = AdvanceTo(SessionState.AwaitingReplacementValue);
        session.TransitionTo(SessionState.Discovering);
        Assert.Equal(SessionState.Discovering, session.State);
    }

    [Fact]
    public void Discovering_CanCompleteAfterACommittedBatch()
    {
        var session = AdvanceTo(SessionState.Discovering);
        session.TransitionTo(SessionState.AwaitingReplacementValue);
        session.TransitionTo(SessionState.Executing);
        session.TransitionTo(SessionState.AwaitingReplacementValue);
        session.TransitionTo(SessionState.Discovering);
        session.TransitionTo(SessionState.Completed);
        Assert.Equal(SessionState.Completed, session.State);
    }

    [Fact]
    public void Started_CannotSkipToExecuting()
    {
        var session = new Session();
        Assert.Throws<InvalidOperationException>(() => session.TransitionTo(SessionState.Executing));
    }

    [Fact]
    public void Completed_IsTerminal()
    {
        var session = AdvanceTo(SessionState.Completed);
        Assert.Throws<InvalidOperationException>(() => session.TransitionTo(SessionState.Cancelled));
        Assert.Throws<InvalidOperationException>(() => session.TransitionTo(SessionState.Discovering));
    }

    [Fact]
    public void Blocked_IsTerminal()
    {
        var session = AdvanceTo(SessionState.Blocked);
        Assert.Throws<InvalidOperationException>(() => session.TransitionTo(SessionState.Cancelled));
    }

    private static Session AdvanceTo(SessionState target)
    {
        var session = new Session();
        foreach (var next in PathTo(target))
            session.TransitionTo(next);
        return session;
    }

    private static IEnumerable<SessionState> PathTo(SessionState target) => target switch
    {
        SessionState.Started => [],
        SessionState.Discovering => [SessionState.Discovering],
        SessionState.AwaitingReplacementValue =>
            [SessionState.Discovering, SessionState.AwaitingReplacementValue],
        SessionState.Executing =>
            [SessionState.Discovering, SessionState.AwaitingReplacementValue, SessionState.Executing],
        SessionState.Completed =>
            [SessionState.Discovering, SessionState.AwaitingReplacementValue, SessionState.Executing, SessionState.AwaitingReplacementValue, SessionState.Completed],
        SessionState.Blocked =>
            [SessionState.Discovering, SessionState.AwaitingReplacementValue, SessionState.Executing, SessionState.Blocked],
        SessionState.Cancelled => [SessionState.Cancelled],
        _ => throw new ArgumentOutOfRangeException(nameof(target), target, null)
    };
}
