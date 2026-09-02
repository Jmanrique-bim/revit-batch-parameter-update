using BatchParamUpdate.Domain.Model;

namespace BatchParamUpdate.Domain.Ports;

/// <summary>
/// Writes a batch-update skip report to durable storage outside the process,
/// so a summary that has grown past what fits comfortably on screen can still
/// be handed to someone who is not in front of the model.
/// </summary>
public interface IReportExportPort
{
    /// <summary>
    /// Exports the given skips as a CSV file and returns the path written.
    /// </summary>
    string ExportSkips(IReadOnlyList<ElementSkip> skips, string runId);
}
