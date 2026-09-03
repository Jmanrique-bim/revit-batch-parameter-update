using BatchParamUpdate.Application.Observability;
using BatchParamUpdate.Application.UseCases;
using BatchParamUpdate.Application.Workflow;
using BatchParamUpdate.Domain.Model;

namespace BatchParamUpdate.Tests.Unit.Fakes;

/// <summary>
/// Builds a <see cref="BatchUpdateCoordinator"/> wired to in-memory fakes so the whole flow
/// (selection → discovery → choose → value → run → complete) can be exercised without Revit.
/// </summary>
public sealed class CoordinatorHarness
{
    public FakeElementSelectionPort Selection { get; } = new();
    public FakeParameterDiscoveryPort Discovery { get; } = new();
    public FakeParameterWritePort Write { get; } = new();
    public FakeSessionRecorderPort Recorder { get; } = new();
    public FakeLoggerPort Logger { get; } = new();

    public SessionTraceListener Trace { get; }
    public Session Session { get; } = new();
    public WorkflowState State { get; } = new();
    public BatchUpdateCoordinator Coordinator { get; }

    public CoordinatorHarness()
    {
        Trace = new SessionTraceListener(
            Recorder, Logger, new SessionRecord("run1", "Doc", DateTimeOffset.UtcNow));
        Coordinator = new BatchUpdateCoordinator(
            Session,
            State,
            new EstablishSelectionUseCase(Selection),
            new DiscoverParametersUseCase(Discovery),
            new RunBatchUpdateUseCase(Write),
            Trace,
            "session-1");
    }

    public void WithPreExisting(params ElementRef[] refs)
        => Selection.PreExisting = new SelectionContext(refs, SelectionOrigin.PreExisting);

    public void WithDiscovered(params string[] names)
        => Discovery.Set = new ParameterCandidateSet(names.Select(n => new ParameterCandidate(n, [])));
}
