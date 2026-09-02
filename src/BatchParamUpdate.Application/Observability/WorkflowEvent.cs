using BatchParamUpdate.Domain.ErrorCatalog;
using BatchParamUpdate.Domain.Model;

namespace BatchParamUpdate.Application.Observability;

/// <summary>
/// A fact the flow announces. The coordinator raises these at each step; a single observer
/// (<see cref="SessionTraceListener"/>) turns them into the <c>.txt</c> log and NDJSON metrics.
/// The flow logic no longer contains any logging calls.
/// </summary>
public abstract record WorkflowEvent
{
    public sealed record SessionStarted(string SessionId) : WorkflowEvent;

    public sealed record SelectionEstablished(SelectionOrigin Origin, int Count) : WorkflowEvent;

    public sealed record ParametersDiscovered(int Count, long ElapsedMs) : WorkflowEvent;

    public sealed record SearchRan(string Text, IReadOnlyList<string> Matches) : WorkflowEvent;

    public sealed record ParameterChosen(string Name) : WorkflowEvent;

    public sealed record BatchStarting(string ParameterName, string NewValue, int ScopeCount) : WorkflowEvent;

    public sealed record BatchFinished(BatchExecutionResult Result, SelectionContext Scope, long ElapsedMs) : WorkflowEvent;

    public sealed record FlowBlocked(ErrorCode Code) : WorkflowEvent;

    public sealed record StateChanged(SessionState From, SessionState To, string Cause) : WorkflowEvent;

    public sealed record SessionEnded(SessionState FinalState) : WorkflowEvent;
}
