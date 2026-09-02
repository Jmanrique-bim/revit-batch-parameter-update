using BatchParamUpdate.Application.Observability;
using BatchParamUpdate.Application.UseCases;
using BatchParamUpdate.Core;
using BatchParamUpdate.Domain.ErrorCatalog;
using BatchParamUpdate.Domain.Model;

namespace BatchParamUpdate.Application.Workflow;

/// <summary>
/// The single owner of the batch-update flow. It is the only component that advances the
/// <see cref="Session"/>, the only caller of the use cases, and the only source of
/// <see cref="WorkflowEvent"/>s. View-models talk to it and read <see cref="State"/>.
/// </summary>
public sealed class BatchUpdateCoordinator
{
    private readonly Session _session;
    private readonly EstablishSelectionUseCase _establish;
    private readonly DiscoverParametersUseCase _discover;
    private readonly RunBatchUpdateUseCase _run;
    private readonly IWorkflowObserver _observer;
    private bool _batchRan;

    public BatchUpdateCoordinator(
        Session session,
        WorkflowState state,
        EstablishSelectionUseCase establish,
        DiscoverParametersUseCase discover,
        RunBatchUpdateUseCase run,
        IWorkflowObserver? observer = null,
        string sessionId = "session")
    {
        _session = session;
        State = state;
        _establish = establish;
        _discover = discover;
        _run = run;
        _observer = observer ?? NullWorkflowObserver.Instance;
        SessionId = sessionId;
        _observer.On(new WorkflowEvent.SessionStarted(sessionId));
    }

    public string SessionId { get; }

    public WorkflowState State { get; }

    public SessionState Step => _session.State;

    public ParameterCandidateSet Candidates { get; private set; } = new([]);

    public ErrorCode? LastError { get; private set; }

    public BatchExecutionResult? LastResult { get; private set; }

    /// <summary>Raised after any coordinator operation so view-models can re-read state.</summary>
    public event Action? Changed;

    public SelectionResult EstablishSelection()
    {
        var preExisting = _establish.DetectPreExisting();
        if (preExisting.IsValid)
        {
            State.SetScope(preExisting);
            Rediscover();
            _observer.On(new WorkflowEvent.SelectionEstablished(preExisting.Origin, preExisting.ElementRefs.Count));
            Changed?.Invoke();
            return new SelectionResult.Established(preExisting);
        }

        return new SelectionResult.NeedsManualPick();
    }

    public void AdoptManualSelection(SelectionContext picked)
    {
        if (!picked.IsValid)
            return;

        State.SetScope(picked);
        Rediscover();
        _observer.On(new WorkflowEvent.SelectionEstablished(picked.Origin, picked.ElementRefs.Count));
        Changed?.Invoke();
    }

    public bool ChooseParameter(ParameterCandidate candidate)
    {
        if (!State.Scope.IsValid)
        {
            Block(ErrorCode.EmptySelection);
            return false;
        }

        var before = _session.State;
        var operation = _discover.Choose(candidate, State.Scope, _session);
        EmitStateChange(before, "choose");

        if (operation is null)
        {
            LastError = _discover.Error;
            if (_discover.Error is { } code)
                _observer.On(new WorkflowEvent.FlowBlocked(code));
            Changed?.Invoke();
            return false;
        }

        LastError = null;
        State.SetTarget(candidate);
        _observer.On(new WorkflowEvent.ParameterChosen(candidate.Name));
        Changed?.Invoke();
        return true;
    }

    public void SetValue(string text) => State.SetNewValue(text);

    public void RecordSearch(string text, IReadOnlyList<string> matches)
        => _observer.On(new WorkflowEvent.SearchRan(text, matches));

    /// <summary>
    /// Snapshots the current scope, target and value into an operation the caller can run later.
    /// The modeless window defers the write to an ExternalEvent, and <see cref="State"/> can
    /// change between the user's click and that callback — the snapshot is what was confirmed.
    /// Returns null (and blocks) when scope or target is missing.
    /// </summary>
    public ReplacementOperation? PrepareRun()
    {
        if (!State.Scope.IsValid)
        {
            Block(ErrorCode.EmptySelection);
            return null;
        }

        if (State.Target is null)
        {
            Block(ErrorCode.NoParameterSelected);
            return null;
        }

        return new ReplacementOperation(State.Target, State.NewValue, State.Scope);
    }

    public BatchExecutionResult? Run(IProgress<BatchProgress>? progress = null)
        => PrepareRun() is { } operation ? Run(operation, progress) : null;

    public BatchExecutionResult? Run(ReplacementOperation operation, IProgress<BatchProgress>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(operation);

        _observer.On(new WorkflowEvent.BatchStarting(
            operation.TargetParameter.Name, operation.NewValue, operation.Scope.ElementRefs.Count));

        var before = _session.State;
        using var timer = PhaseTimer.Start();
        LastResult = _run.Execute(_session, operation, operation.Scope, progress ?? new Progress<BatchProgress>());
        var elapsed = timer.ElapsedMs;
        EmitStateChange(before, "run");

        LastError = _run.Error;
        if (LastResult is not null)
        {
            _observer.On(new WorkflowEvent.BatchFinished(LastResult, operation.Scope, elapsed));
            if (LastResult.RolledBack)
                _observer.On(new WorkflowEvent.FlowBlocked(ErrorCode.BatchRolledBack));
            else
                _batchRan = true;
        }
        else if (_run.Error is { } code)
        {
            _observer.On(new WorkflowEvent.FlowBlocked(code));
        }

        Changed?.Invoke();
        return LastResult;
    }

    /// <summary>Resolve the terminal session state when the window closes.</summary>
    public void Complete()
    {
        var from = _session.State;
        if (_session.State is SessionState.AwaitingReplacementValue && _batchRan)
            _session.TransitionTo(SessionState.Completed);
        else if (_session.State is not SessionState.Completed
                 and not SessionState.Blocked
                 and not SessionState.Cancelled)
            _session.TransitionTo(SessionState.Cancelled);

        EmitStateChange(from, "close");
        _observer.On(new WorkflowEvent.SessionEnded(_session.State));
    }

    private void Block(ErrorCode code)
    {
        LastError = code;
        _observer.On(new WorkflowEvent.FlowBlocked(code));
        Changed?.Invoke();
    }

    private void Rediscover()
    {
        using var timer = PhaseTimer.Start();
        Candidates = _discover.Discover(State.Scope);
        _observer.On(new WorkflowEvent.ParametersDiscovered(Candidates.Candidates.Count, timer.ElapsedMs));

        if (_session.State == SessionState.Started && State.Scope.IsValid)
        {
            _session.TransitionTo(SessionState.Discovering);
            _observer.On(new WorkflowEvent.StateChanged(SessionState.Started, SessionState.Discovering, "scope"));
        }
    }

    private void EmitStateChange(SessionState from, string cause)
    {
        if (from != _session.State)
            _observer.On(new WorkflowEvent.StateChanged(from, _session.State, cause));
    }
}
