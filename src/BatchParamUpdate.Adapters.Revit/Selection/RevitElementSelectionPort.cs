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
            refs.Add(ToRef(_uidoc.Document.GetElement(id), id));

        return new SelectionContext(refs, SelectionOrigin.PreExisting);
    }

    public SelectionContext? PromptManualSelection()
    {
        try
        {
            var picked = _uidoc.Selection.PickObjects(ObjectType.Element, "Select elements");
            var refs = new List<ElementRef>(picked.Count);
            foreach (var reference in picked)
                refs.Add(ToRef(_uidoc.Document.GetElement(reference.ElementId), reference.ElementId));

            return new SelectionContext(refs, SelectionOrigin.ManualPick);
        }
        catch (Autodesk.Revit.Exceptions.OperationCanceledException)
        {
            return null;
        }
    }

    private static ElementRef ToRef(Element? element, ElementId id)
    {
        var category = element?.Category?.Name ?? "";
        var typeName = "";
        if (element is not null)
        {
            var typeId = element.GetTypeId();
            typeName = typeId != ElementId.InvalidElementId
                ? element.Document.GetElement(typeId)?.Name ?? ""
                : element.Name ?? "";
        }

        return new ElementRef(id.ToString(), category, typeName);
    }
}
