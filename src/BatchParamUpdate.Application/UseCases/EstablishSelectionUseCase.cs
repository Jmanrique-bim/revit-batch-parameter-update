using BatchParamUpdate.Domain.Model;
using BatchParamUpdate.Domain.Ports;

namespace BatchParamUpdate.Application.UseCases;

public sealed class EstablishSelectionUseCase
{
    private readonly IElementSelectionPort _selection;

    public EstablishSelectionUseCase(IElementSelectionPort selection)
        => _selection = selection;

    public SelectionContext Execute(Session session)
    {
        ArgumentNullException.ThrowIfNull(session);

        var context = _selection.GetPreExistingSelection();
        if (context.IsValid)
            session.TransitionTo(SessionState.Discovering);

        return context;
    }
}
