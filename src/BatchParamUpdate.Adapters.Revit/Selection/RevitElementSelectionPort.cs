using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
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

    public SelectionContext? PromptManualSelection()
    {
        try
        {
            var picked = _uidoc.Selection.PickObjects(ObjectType.Element, "Select elements");
            var refs = new List<ElementRef>(picked.Count);
            foreach (var reference in picked)
            {
                var element = _uidoc.Document.GetElement(reference.ElementId);
                refs.Add(new ElementRef(
                    reference.ElementId.ToString(),
                    element?.Category?.Name ?? string.Empty));
            }

            return new SelectionContext(refs, SelectionOrigin.ManualPick);
        }
        catch (Autodesk.Revit.Exceptions.OperationCanceledException)
        {
            return null;
        }
    }
}
