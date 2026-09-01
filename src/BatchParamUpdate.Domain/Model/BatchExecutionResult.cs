namespace BatchParamUpdate.Domain.Model;

public sealed record InstanceOutcome(int UpdatedCount, IReadOnlyList<ElementSkip> Skips);

public sealed record TypeOutcome(IReadOnlyList<ResolvedType> AffectedTypes, int TotalElementsUpdated);

public sealed class BatchExecutionResult
{
    private BatchExecutionResult(
        ParameterBinding path,
        InstanceOutcome? instanceOutcome,
        TypeOutcome? typeOutcome)
    {
        Path = path;
        InstanceOutcome = instanceOutcome;
        TypeOutcome = typeOutcome;
    }

    public ParameterBinding Path { get; }

    public InstanceOutcome? InstanceOutcome { get; }

    public TypeOutcome? TypeOutcome { get; }

    public static BatchExecutionResult ForInstance(int updatedCount, IReadOnlyList<ElementSkip> skips)
        => new(ParameterBinding.Instance, new InstanceOutcome(updatedCount, skips), typeOutcome: null);

    public static BatchExecutionResult ForType(IReadOnlyList<ResolvedType> affectedTypes, int totalElementsUpdated)
        => new(ParameterBinding.Type, instanceOutcome: null, new TypeOutcome(affectedTypes, totalElementsUpdated));
}
