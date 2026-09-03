using BatchParamUpdate.Domain.ErrorCatalog;
using BatchParamUpdate.Domain.Model;
using BatchParamUpdate.Domain.Ports;

namespace BatchParamUpdate.Application.UseCases;

public sealed class DiscoverParametersUseCase
{
    private readonly IParameterDiscoveryPort _discovery;

    public DiscoverParametersUseCase(IParameterDiscoveryPort discovery)
        => _discovery = discovery;

    public ErrorCode? Error { get; private set; }

    public ParameterCandidateSet Discover(SelectionContext scope)
        => _discovery.Discover(scope);

    public ReplacementOperation? Choose(ParameterCandidate? candidate, SelectionContext scope, Session session)
    {
        if (candidate is null)
        {
            Error = ErrorCode.NoParameterSelected;
            return null;
        }

        Error = null;

        if (session.State == SessionState.Started && scope.IsValid)
            session.TransitionTo(SessionState.Discovering);

        if (session.State == SessionState.Discovering)
            session.TransitionTo(SessionState.AwaitingReplacementValue);

        if (session.State != SessionState.AwaitingReplacementValue)
            return null;

        return new ReplacementOperation(candidate, newValue: "", scope);
    }
}
