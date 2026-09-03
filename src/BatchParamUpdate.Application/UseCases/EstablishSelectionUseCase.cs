using BatchParamUpdate.Domain.Model;
using BatchParamUpdate.Domain.Ports;

namespace BatchParamUpdate.Application.UseCases;

public sealed class EstablishSelectionUseCase
{
    private readonly IElementSelectionPort _selection;

    public EstablishSelectionUseCase(IElementSelectionPort selection)
        => _selection = selection;

    /// <summary>
    /// The selection present in the active document when the command launched. May be empty —
    /// the caller then opens the window with manual pick enabled (User Story 2).
    /// </summary>
    public SelectionContext DetectPreExisting() => _selection.GetPreExistingSelection();
}
