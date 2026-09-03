namespace BatchParamUpdate.Domain.Model;

public enum ParameterStorageKind
{
    None,
    Text,
    NonText
}

/// <summary>Everything the write path reads about one element+parameter before deciding.</summary>
public readonly record struct ParameterState(
    bool ElementFound,
    bool InModelGroup,
    WorkshareStatus Workshare,
    bool ParameterFound,
    bool IsReadOnly,
    ParameterStorageKind Storage);

public abstract record WriteOutcome
{
    public sealed record Updated : WriteOutcome;

    public sealed record Skip(SkipReason Reason) : WriteOutcome;
}

/// <summary>
/// The whole "update or skip, and why" decision, isolated from Revit so every branch is unit
/// tested. <paramref name="trySet"/> is the actual <c>Parameter.Set</c> call — invoked only once
/// every precondition has passed, and its <c>false</c> return (a silent Revit rejection) is
/// turned into a recorded skip instead of a phantom success.
/// </summary>
public static class ParameterWriteDecision
{
    public static WriteOutcome Evaluate(ParameterState state, Func<bool> trySet)
    {
        if (!state.ElementFound)
            return new WriteOutcome.Skip(SkipReason.ElementNotFound);
        if (state.InModelGroup)
            return new WriteOutcome.Skip(SkipReason.ModelGroupMember);
        if (state.Workshare == WorkshareStatus.OwnedByOtherUser)
            return new WriteOutcome.Skip(SkipReason.WorksharingOwnedByOther);
        if (!state.ParameterFound)
            return new WriteOutcome.Skip(SkipReason.ParameterMissing);
        if (state.IsReadOnly)
            return new WriteOutcome.Skip(SkipReason.ParameterReadOnly);
        if (state.Storage != ParameterStorageKind.Text)
            return new WriteOutcome.Skip(SkipReason.ParameterNotText);

        return trySet()
            ? new WriteOutcome.Updated()
            : new WriteOutcome.Skip(SkipReason.ValueRejected);
    }
}
