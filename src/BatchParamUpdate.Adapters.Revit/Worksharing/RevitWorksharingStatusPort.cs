using Autodesk.Revit.DB;
using BatchParamUpdate.Domain.Model;
using BatchParamUpdate.Domain.Ports;

namespace BatchParamUpdate.Adapters.Revit.Worksharing;

public sealed class RevitWorksharingStatusPort : IWorksharingStatusPort
{
    private readonly Document _doc;

    public RevitWorksharingStatusPort(Document doc) => _doc = doc;

    public WorkshareStatus GetWorkshareStatus(ElementRef element)
    {
        if (!_doc.IsWorkshared || !long.TryParse(element.Id, out var value))
            return WorkshareStatus.NotWorkshared;

        return WorksharingUtils.GetCheckoutStatus(_doc, new ElementId(value)) switch
        {
            CheckoutStatus.OwnedByCurrentUser => WorkshareStatus.OwnedByCurrentUser,
            CheckoutStatus.OwnedByOtherUser => WorkshareStatus.OwnedByOtherUser,
            _ => WorkshareStatus.NotWorkshared
        };
    }
}
