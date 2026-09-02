using BatchParamUpdate.Domain.Model;
using BatchParamUpdate.Domain.Ports;

namespace BatchParamUpdate.Application.UseCases;

/// <summary>
/// Thin wrapper around <see cref="IReportExportPort"/> so the UI layer never
/// touches the filesystem directly — it depends on this use case, same as
/// every other write in the app.
/// </summary>
public sealed class ExportSkipReportUseCase
{
    private readonly IReportExportPort _export;

    public ExportSkipReportUseCase(IReportExportPort export)
    {
        _export = export;
    }

    /// <summary>
    /// Exports the skips to CSV. Returns null when there is nothing to export
    /// so the caller can leave the button disabled instead of writing an
    /// empty file.
    /// </summary>
    public string? Execute(IReadOnlyList<ElementSkip> skips, string runId)
    {
        if (skips.Count == 0)
            return null;

        return _export.ExportSkips(skips, runId);
    }
}
