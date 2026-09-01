using BatchParamUpdate.Application.UseCases;
using BatchParamUpdate.Domain.Model;
using BatchParamUpdate.Tests.Unit.Fakes;
using Xunit;

namespace BatchParamUpdate.Tests.Unit.Application;

public sealed class EstablishSelectionUseCaseTests
{
    [Fact]
    public void Execute_WhenPreExistingElements_AdoptsThemWithoutManualPick()
    {
        var preExisting = new SelectionContext(
            [new ElementRef("42", "Walls"), new ElementRef("43", "Doors")],
            SelectionOrigin.PreExisting);
        var port = new FakeElementSelectionPort { PreExisting = preExisting };
        var useCase = new EstablishSelectionUseCase(port);
        var session = new Session();

        var result = useCase.Execute(session);

        Assert.Same(preExisting, result);
        Assert.True(result.IsValid);
        Assert.Equal(SelectionOrigin.PreExisting, result.Origin);
        Assert.Equal(0, port.PromptManualSelectionCalls);
        Assert.Equal(SessionState.Discovering, session.State);
    }

    [Fact]
    public void Execute_WhenNoPreExisting_InvokesPromptManualSelection()
    {
        var picked = new SelectionContext(
            [new ElementRef("7", "Walls")],
            SelectionOrigin.ManualPick);
        var port = new FakeElementSelectionPort
        {
            PreExisting = new SelectionContext([], SelectionOrigin.PreExisting),
            Manual = picked
        };
        var useCase = new EstablishSelectionUseCase(port);
        var session = new Session();

        var result = useCase.Execute(session);

        Assert.Same(picked, result);
        Assert.Equal(1, port.PromptManualSelectionCalls);
        Assert.Equal(SelectionOrigin.ManualPick, result.Origin);
        Assert.Equal(SessionState.Discovering, session.State);
    }

    [Fact]
    public void Execute_WhenManualPickCancelled_LeavesSessionWithoutValidScope()
    {
        var port = new FakeElementSelectionPort
        {
            PreExisting = new SelectionContext([], SelectionOrigin.PreExisting),
            Manual = null
        };
        var useCase = new EstablishSelectionUseCase(port);
        var session = new Session();

        var result = useCase.Execute(session);

        Assert.Equal(1, port.PromptManualSelectionCalls);
        Assert.False(result.IsValid);
        Assert.Equal(SelectionOrigin.ManualPick, result.Origin);
        Assert.Equal(SessionState.Started, session.State);
    }
}
