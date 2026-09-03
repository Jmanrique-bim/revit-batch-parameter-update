namespace BatchParamUpdate.Domain.Ports;

/// <summary>Suppresses native Revit dialogs for the duration of a batch write.</summary>
public interface INativeDialogSuppressionPort
{
    IDisposable SuppressNativeDialogsDuringBatch();
}
