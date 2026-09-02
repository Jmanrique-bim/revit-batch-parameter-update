using BatchParamUpdate.Domain.Model;
using BatchParamUpdate.Domain.Ports;

namespace BatchParamUpdate.Tests.Unit.Fakes;

public sealed class FakeReportExportPort : IReportExportPort
{
    public List<(IReadOnlyList<ElementSkip> Skips, string RunId)> Calls { get; } = [];

    public string PathToReturn { get; set; } = "C:\\temp\\skip-report.csv";

    public string ExportSkips(IReadOnlyList<ElementSkip> skips, string runId)
    {
        Calls.Add((skips, runId));
        return PathToReturn;
    }
}
