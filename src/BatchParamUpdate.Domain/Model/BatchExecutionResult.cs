namespace BatchParamUpdate.Domain.Model;

/// <summary>
/// Outcome of running a batch instance-parameter update: how many elements were written, which
/// were skipped and why, and whether the enclosing transaction actually committed.
/// </summary>
public sealed record BatchExecutionResult(
    int UpdatedCount,
    IReadOnlyList<ElementSkip> Skips,
    bool RolledBack)
{
    public static BatchExecutionResult Committed(int updatedCount, IReadOnlyList<ElementSkip> skips)
        => new(updatedCount, skips, RolledBack: false);

    public static BatchExecutionResult Reverted(IReadOnlyList<ElementSkip> skips)
        => new(UpdatedCount: 0, skips, RolledBack: true);
}
