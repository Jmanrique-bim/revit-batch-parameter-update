using BatchParamUpdate.Application.UseCases;
using BatchParamUpdate.Domain.Model;
using BatchParamUpdate.Tests.Unit.Fakes;
using Xunit;

namespace BatchParamUpdate.Tests.Unit.Application;

public sealed class ExportSkipReportUseCaseTests
{
    [Fact]
    public void Execute_ReturnsNull_AndDoesNotCallPort_WhenNoSkips()
    {
        var port = new FakeReportExportPort();
        var useCase = new ExportSkipReportUseCase(port);

        var result = useCase.Execute([], "run-1");

        Assert.Null(result);
        Assert.Empty(port.Calls);
    }

    [Fact]
    public void Execute_DelegatesToPort_AndReturnsItsPath_WhenSkipsPresent()
    {
        var port = new FakeReportExportPort { PathToReturn = "C:\\temp\\skip-report-run-1.csv" };
        var useCase = new ExportSkipReportUseCase(port);
        var skips = new[]
        {
            ElementSkip.Create(new ElementRef("1", "Doors"), SkipReason.ParameterReadOnly),
            ElementSkip.Create(new ElementRef("2", "Walls"), SkipReason.WorksharingOwnedByOther),
        };

        var result = useCase.Execute(skips, "run-1");

        Assert.Equal("C:\\temp\\skip-report-run-1.csv", result);
        var call = Assert.Single(port.Calls);
        Assert.Equal("run-1", call.RunId);
        Assert.Equal(2, call.Skips.Count);
    }
}
