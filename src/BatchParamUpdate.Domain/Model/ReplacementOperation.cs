namespace BatchParamUpdate.Domain.Model;

public sealed class ReplacementOperation
{
    public ReplacementOperation(
        ParameterCandidate targetParameter,
        string newValue,
        ExecutionScope executionScope)
    {
        TargetParameter = targetParameter;
        NewValue = newValue;
        ExecutionScope = executionScope;
    }

    public ParameterCandidate TargetParameter { get; }

    public string NewValue { get; }

    public bool RequiresWideBlastRadiusWarning => TargetParameter.Binding == ParameterBinding.Type;

    public ExecutionScope ExecutionScope { get; }

    public bool HasReplacementValue => !string.IsNullOrWhiteSpace(NewValue);

    public ReplacementOperation WithNewValue(string newValue)
        => new(TargetParameter, newValue, ExecutionScope);
}
