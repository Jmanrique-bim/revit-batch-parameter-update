using BatchParamUpdate.Domain.Model;
using Xunit;

namespace BatchParamUpdate.Tests.Unit.Domain;

public sealed class BatchExecutionResultTests
{
    [Fact]
    public void InstanceOutcome_AndTypeOutcome_AreMutuallyExclusiveByPath()
    {
        var instance = BatchExecutionResult.ForInstance(3, []);
        Assert.Equal(ParameterBinding.Instance, instance.Path);
        Assert.NotNull(instance.InstanceOutcome);
        Assert.Null(instance.TypeOutcome);

        var type = BatchExecutionResult.ForType(
            [new ResolvedType("t1", "Basic Wall", [])],
            10);
        Assert.Equal(ParameterBinding.Type, type.Path);
        Assert.NotNull(type.TypeOutcome);
        Assert.Null(type.InstanceOutcome);
    }
}
