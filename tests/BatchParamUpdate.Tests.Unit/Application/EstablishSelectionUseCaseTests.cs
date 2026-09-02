using BatchParamUpdate.Application.UseCases;
using BatchParamUpdate.Domain.Model;
using BatchParamUpdate.Tests.Unit.Fakes;
using Xunit;

namespace BatchParamUpdate.Tests.Unit.Application;

public sealed class EstablishSelectionUseCaseTests
{
    [Fact]
    public void DetectPreExisting_ReturnsWhateverThePortReports_WhenPopulated()
    {
        var preExisting = new SelectionContext(
            [new ElementRef("42", "Walls"), new ElementRef("43", "Doors")],
            SelectionOrigin.PreExisting);
        var useCase = new EstablishSelectionUseCase(new FakeElementSelectionPort { PreExisting = preExisting });

        var result = useCase.DetectPreExisting();

        Assert.Same(preExisting, result);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void DetectPreExisting_ReturnsEmptyContext_WhenNothingSelected()
    {
        var useCase = new EstablishSelectionUseCase(new FakeElementSelectionPort
        {
            PreExisting = new SelectionContext([], SelectionOrigin.PreExisting)
        });

        var result = useCase.DetectPreExisting();

        Assert.False(result.IsValid);
    }
}
