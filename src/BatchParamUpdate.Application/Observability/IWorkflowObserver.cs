namespace BatchParamUpdate.Application.Observability;

/// <summary>Receives every <see cref="WorkflowEvent"/> the coordinator raises.</summary>
public interface IWorkflowObserver
{
    void On(WorkflowEvent workflowEvent);
}

/// <summary>Default no-op observer, for callers that do not want tracing.</summary>
public sealed class NullWorkflowObserver : IWorkflowObserver
{
    public static readonly NullWorkflowObserver Instance = new();

    public void On(WorkflowEvent workflowEvent)
    {
    }
}
