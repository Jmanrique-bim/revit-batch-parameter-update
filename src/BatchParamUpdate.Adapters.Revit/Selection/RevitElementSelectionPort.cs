using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BatchParamUpdate.Domain.Model;
using BatchParamUpdate.Domain.Ports;

namespace BatchParamUpdate.Adapters.Revit.Selection;

public sealed class RevitElementSelectionPort : IElementSelectionPort
{
    private readonly UIDocument _uidoc;

    public RevitElementSelectionPort(UIDocument uidoc) => _uidoc = uidoc;

    public SelectionContext GetPreExistingSelection()
    {
        var refs = new List<ElementRef>();
        foreach (var id in _uidoc.Selection.GetElementIds())
        {
            var element = _uidoc.Document.GetElement(id);
            refs.Add(new ElementRef(id.ToString(), element?.Category?.Name ?? string.Empty));
        }

        return new SelectionContext(refs, SelectionOrigin.PreExisting);
    }

    // ponytail: T052 (US2) implements PickObjects; US1 never calls this path.
    public SelectionContext? PromptManualSelection() => null;
}
