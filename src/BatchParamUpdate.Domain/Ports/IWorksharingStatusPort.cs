using BatchParamUpdate.Domain.Model;

namespace BatchParamUpdate.Domain.Ports;

/// <summary>Reports whether a workshared element is checked out by another user.</summary>
public interface IWorksharingStatusPort
{
    WorkshareStatus GetWorkshareStatus(ElementRef element);
}
