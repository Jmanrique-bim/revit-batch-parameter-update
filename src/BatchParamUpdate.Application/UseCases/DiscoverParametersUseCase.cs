using BatchParamUpdate.Core;
using BatchParamUpdate.Domain.ErrorCatalog;
using BatchParamUpdate.Domain.Model;
using BatchParamUpdate.Domain.Ports;

namespace BatchParamUpdate.Application.UseCases;

public sealed class DiscoverParametersUseCase
{
    private readonly IParameterDiscoveryPort _discovery;
    private readonly RecordSessionUseCase? _recorder;

    public DiscoverParametersUseCase(IParameterDiscoveryPort discovery, RecordSessionUseCase? recorder = null)
    {
        _discovery = discovery;
        _recorder = recorder;
    }

    public ErrorCode? Error { get; private set; }

    public (InstanceParameterCandidateSet Instance, TypeParameterCandidateSet Type) Discover(SelectionContext scope)
    {
        using var timer = PhaseTimer.Start();
        var instance = _discovery.DiscoverInstanceCandidates(scope);
        var type = _discovery.DiscoverTypeCandidates(scope);
        _recorder?.RecordPhaseTiming("Discovery", timer.ElapsedMs);
        _recorder?.Trace(
            "model",
            "discovery",
            "done",
            ("scope", scope.ElementRefs.Count),
            ("origin", scope.Origin),
            ("instance", instance.Candidates.Count),
            ("type", type.Candidates.Count));
        return (instance, type);
    }

    public ReplacementOperation? Choose(
        ParameterCandidate? candidate,
        SelectionContext scope,
        Session session)
    {
        if (candidate is null)
        {
            Error = ErrorCode.NoParameterSelected;
            _recorder?.Trace("model", "param", "blocked", ("reason", "noParameter"), ("session", session.State));
            return null;
        }

        Error = null;

        var from = session.State;
        if (session.State == SessionState.Started && scope.IsValid)
            session.TransitionTo(SessionState.Discovering);
        _recorder?.TraceState(from, session, "choose");

        from = session.State;
        if (session.State == SessionState.Discovering)
            session.TransitionTo(SessionState.AwaitingReplacementValue);
        _recorder?.TraceState(from, session, "choose");

        if (session.State != SessionState.AwaitingReplacementValue)
        {
            _recorder?.Trace(
                "model",
                "param",
                "blocked",
                ("reason", "session"),
                ("session", session.State),
                ("scopeValid", scope.IsValid));
            return null;
        }

        ExecutionScope execution = candidate.Binding == ParameterBinding.Instance
            ? new InstanceScope(scope)
            : new TypeScope([]);

        _recorder?.RecordParameterSelected(candidate);
        return new ReplacementOperation(candidate, newValue: "", execution);
    }
}
