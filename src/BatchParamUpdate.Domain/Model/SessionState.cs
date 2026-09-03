namespace BatchParamUpdate.Domain.Model;

public enum SessionState
{
    Started,
    Discovering,
    AwaitingReplacementValue,
    Executing,
    Completed,
    Blocked,
    Cancelled
}
