using BatchParamUpdate.Domain.Model;
using Xunit;

namespace BatchParamUpdate.Tests.Unit.Domain;

public sealed class BatchExecutionResultTests
{
    [Fact]
    public void Committed_CarriesUpdatedCountAndSkips_AndIsNotRolledBack()
    {
        var skip = ElementSkip.Create(new ElementRef("2", "Doors"), SkipReason.ParameterMissing);

        var result = BatchExecutionResult.Committed(3, [skip]);

        Assert.Equal(3, result.UpdatedCount);
        Assert.Single(result.Skips);
        Assert.False(result.RolledBack);
    }

    [Fact]
    public void Reverted_ReportsZeroUpdated_AndIsRolledBack()
    {
        var skip = ElementSkip.Create(new ElementRef("2", "Doors"), SkipReason.ParameterMissing);

        var result = BatchExecutionResult.Reverted([skip]);

        Assert.Equal(0, result.UpdatedCount);
        Assert.Single(result.Skips);
        Assert.True(result.RolledBack);
    }
}
