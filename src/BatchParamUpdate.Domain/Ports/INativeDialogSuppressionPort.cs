using BatchParamUpdate.Domain.Model;

namespace BatchParamUpdate.Domain.Ports;

public interface INativeDialogSuppressionPort
{
    WorkshareStatus GetWorkshareStatus(ElementRef element);

    IDisposable SuppressNativeDialogsDuringBatch();
}
