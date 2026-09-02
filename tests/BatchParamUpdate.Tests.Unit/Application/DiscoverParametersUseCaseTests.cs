using BatchParamUpdate.Application.UseCases;
using BatchParamUpdate.Domain.ErrorCatalog;
using BatchParamUpdate.Domain.Model;
using BatchParamUpdate.Tests.Unit.Fakes;
using Xunit;

namespace BatchParamUpdate.Tests.Unit.Application;

public sealed class DiscoverParametersUseCaseTests
{
    private static readonly SelectionContext Scope = new(
        [new ElementRef("1", "Walls")],
        SelectionOrigin.PreExisting);

    [Fact]
    public void Choose_WhenNoParameterSelected_BlocksAdvanceWithCatalogError()
    {
        var useCase = new DiscoverParametersUseCase(new FakeParameterDiscoveryPort());
        var session = DiscoveringSession();

        var result = useCase.Choose(null, Scope, session);

        Assert.Null(result);
        Assert.Equal(ErrorCode.NoParameterSelected, useCase.Error);
        Assert.Equal(SessionState.Discovering, session.State);
    }

    [Fact]
    public void Choose_WhenStartedWithEmptyScope_DoesNotReturnOperation()
    {
        var candidate = new ParameterCandidate(
            "Comments",
            ParameterBinding.Instance,
            [new ElementRef("1", "Walls")]);
        var empty = new SelectionContext([], SelectionOrigin.ManualPick);
        var useCase = new DiscoverParametersUseCase(new FakeParameterDiscoveryPort());
        var session = new Session();

        var result = useCase.Choose(candidate, empty, session);

        Assert.Null(result);
        Assert.Null(useCase.Error);
        Assert.Equal(SessionState.Started, session.State);
    }

    [Fact]
    public void Choose_TypeCandidate_SetsWideBlastWarningWithoutBlocking()
    {
        var candidate = new ParameterCandidate(
            "Type Comments",
            ParameterBinding.Type,
            [new ElementRef("1", "Walls")]);
        var useCase = new DiscoverParametersUseCase(new FakeParameterDiscoveryPort());
        var session = DiscoveringSession();

        var result = useCase.Choose(candidate, Scope, session);

        Assert.NotNull(result);
        Assert.True(result.RequiresWideBlastRadiusWarning);
        Assert.Null(useCase.Error);
        Assert.Equal(SessionState.AwaitingReplacementValue, session.State);
    }

    private static Session DiscoveringSession()
    {
        var session = new Session();
        session.TransitionTo(SessionState.Discovering);
        return session;
    }
}
