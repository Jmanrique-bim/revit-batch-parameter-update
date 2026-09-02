using BatchParamUpdate.Domain.Model;
using Xunit;

namespace BatchParamUpdate.Tests.Unit.Domain;

public sealed class ParameterWriteDecisionTests
{
    private static ParameterState Writable => new(
        ElementFound: true,
        InModelGroup: false,
        Workshare: WorkshareStatus.NotWorkshared,
        ParameterFound: true,
        IsReadOnly: false,
        Storage: ParameterStorageKind.Text);

    [Fact]
    public void AllPreconditionsPass_AndSetAccepts_IsUpdated()
        => Assert.IsType<WriteOutcome.Updated>(ParameterWriteDecision.Evaluate(Writable, () => true));

    [Fact]
    public void SetSilentlyRejects_IsSkippedAsValueRejected()
    {
        var outcome = ParameterWriteDecision.Evaluate(Writable, () => false);
        Assert.Equal(SkipReason.ValueRejected, Assert.IsType<WriteOutcome.Skip>(outcome).Reason);
    }

    [Theory]
    [MemberData(nameof(SkipCases))]
    public void PreconditionFailures_SkipWithTheRightReason_WithoutCallingSet(ParameterState state, SkipReason expected)
    {
        var setCalled = false;

        var outcome = ParameterWriteDecision.Evaluate(state, () => { setCalled = true; return true; });

        Assert.Equal(expected, Assert.IsType<WriteOutcome.Skip>(outcome).Reason);
        Assert.False(setCalled);
    }

    public static IEnumerable<object[]> SkipCases()
    {
        yield return [Writable with { ElementFound = false }, SkipReason.ElementNotFound];
        yield return [Writable with { InModelGroup = true }, SkipReason.ModelGroupMember];
        yield return [Writable with { Workshare = WorkshareStatus.OwnedByOtherUser }, SkipReason.WorksharingOwnedByOther];
        yield return [Writable with { ParameterFound = false }, SkipReason.ParameterMissing];
        yield return [Writable with { IsReadOnly = true }, SkipReason.ParameterReadOnly];
        yield return [Writable with { Storage = ParameterStorageKind.NonText }, SkipReason.ParameterNotText];
    }

    [Fact]
    public void ModelGroup_TakesPrecedenceOverParameterChecks()
    {
        var state = Writable with { InModelGroup = true, ParameterFound = false, IsReadOnly = true };
        var outcome = ParameterWriteDecision.Evaluate(state, () => true);
        Assert.Equal(SkipReason.ModelGroupMember, Assert.IsType<WriteOutcome.Skip>(outcome).Reason);
    }
}
