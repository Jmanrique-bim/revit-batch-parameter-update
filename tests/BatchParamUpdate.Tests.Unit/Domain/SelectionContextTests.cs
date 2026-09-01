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
}
