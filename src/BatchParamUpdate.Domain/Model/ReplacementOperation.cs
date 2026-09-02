namespace BatchParamUpdate.Domain.Model;

public sealed class ReplacementOperation
{
    public ReplacementOperation(ParameterCandidate targetParameter, string newValue, SelectionContext scope)
    {
        TargetParameter = targetParameter;
        NewValue = newValue;
        Scope = scope;
    }

    public ParameterCandidate TargetParameter { get; }

    public string NewValue { get; }

    public SelectionContext Scope { get; }

    public bool HasReplacementValue => !string.IsNullOrWhiteSpace(NewValue);

    public ReplacementOperation WithNewValue(string newValue)
        => new(TargetParameter, newValue, Scope);
}
