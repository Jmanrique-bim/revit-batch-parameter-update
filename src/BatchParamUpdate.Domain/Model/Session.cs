namespace BatchParamUpdate.Domain.Model;

public sealed class Session
{
    public SessionState State { get; private set; } = SessionState.Started;

    public void TransitionTo(SessionState next)
    {
        if (!IsAllowed(State, next))
            throw new InvalidOperationException($"Cannot transition from {State} to {next}.");

        State = next;
    }

    public static bool IsAllowed(SessionState from, SessionState to)
    {
        if (from is SessionState.Completed or SessionState.Blocked or SessionState.Cancelled)
            return false;

        return to switch
        {
            SessionState.Discovering => from is SessionState.Started or SessionState.AwaitingReplacementValue,
            SessionState.AwaitingReplacementValue => from is SessionState.Discovering or SessionState.Executing,
            SessionState.Executing => from is SessionState.AwaitingReplacementValue,
            SessionState.Completed => from is SessionState.Executing or SessionState.AwaitingReplacementValue,
            SessionState.Blocked => from is SessionState.Executing,
            SessionState.Cancelled => true,
            _ => false
        };
    }
}
