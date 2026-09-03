using BatchParamUpdate.Domain.Model;

namespace BatchParamUpdate.Domain.Ports;

public interface IElementSelectionPort
{
    SelectionContext GetPreExistingSelection();
    SelectionContext? PromptManualSelection();
}
