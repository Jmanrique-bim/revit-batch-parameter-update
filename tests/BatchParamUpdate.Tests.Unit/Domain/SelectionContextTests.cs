using BatchParamUpdate.Domain.Model;
using Xunit;

namespace BatchParamUpdate.Tests.Unit.Domain;

public sealed class SelectionContextTests
{
    [Fact]
    public void IsValid_IsFalse_WhenElementRefsEmpty()
    {
        var context = new SelectionContext([], SelectionOrigin.PreExisting);
        Assert.False(context.IsValid);
    }

    [Fact]
    public void IsValid_IsTrue_WhenElementRefsPresent()
    {
        var context = new SelectionContext(
            [new ElementRef("1", "Walls")],
            SelectionOrigin.PreExisting);
        Assert.True(context.IsValid);
    }

    [Fact]
    public void Origin_ManualPick_LeavesSelectElementsControlEnabled()
    {
        var context = new SelectionContext([], SelectionOrigin.ManualPick);
        Assert.Equal(SelectionOrigin.ManualPick, context.Origin);
        Assert.False(context.IsValid);
        Assert.True(SelectElementsEnabled(context));
    }

    [Fact]
    public void Origin_PreExisting_DisablesSelectElementsControl()
    {
        var context = new SelectionContext(
            [new ElementRef("1", "Walls")],
            SelectionOrigin.PreExisting);
        Assert.False(SelectElementsEnabled(context));
    }

    // ponytail: Domain cannot reference WPF; this is the FR-003/FR-004 rule SelectElementsViewModel applies.
    private static bool SelectElementsEnabled(SelectionContext context)
        => context.Origin == SelectionOrigin.ManualPick;
}
