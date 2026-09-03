using BatchParamUpdate.Domain.Model;
using BatchParamUpdate.Domain.Ports;

namespace BatchParamUpdate.Tests.Unit.Fakes;

public sealed class FakeElementSelectionPort : IElementSelectionPort
{
    public SelectionContext PreExisting { get; set; } =
        new([], SelectionOrigin.PreExisting);

    public SelectionContext? Manual { get; set; }

    public int PromptManualSelectionCalls { get; private set; }

    public SelectionContext GetPreExistingSelection() => PreExisting;

    public SelectionContext? PromptManualSelection()
    {
        PromptManualSelectionCalls++;
        return Manual;
    }
}
