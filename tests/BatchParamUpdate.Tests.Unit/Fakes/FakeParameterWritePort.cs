using BatchParamUpdate.Domain.Model;
using BatchParamUpdate.Domain.Ports;

namespace BatchParamUpdate.Tests.Unit.Fakes;

public sealed class FakeParameterWritePort : IParameterWritePort
{
    public bool BlockGlobally { get; set; }

    public Dictionary<string, SkipReason> SkipsByElementId { get; } = new();

    public BatchExecutionResult? TypeUpdateResult { get; set; }

    public int InstanceUpdateCalls { get; private set; }

    public int TypeUpdateCalls { get; private set; }

    public BatchExecutionResult? ExecuteInstanceUpdate(
        SelectionContext scope,
        ParameterCandidate targetParameter,
        string newValue)
    {
        InstanceUpdateCalls++;
        if (BlockGlobally)
            return null;

        var skips = scope.ElementRefs
            .Where(e => SkipsByElementId.ContainsKey(e.Id))
            .Select(e => ElementSkip.Create(e, SkipsByElementId[e.Id]))
            .ToList();
        return BatchExecutionResult.ForInstance(scope.ElementRefs.Count - skips.Count, skips);
    }

    public BatchExecutionResult? ExecuteTypeUpdate(
        SelectionContext scope,
        ParameterCandidate targetParameter,
        string newValue)
    {
        TypeUpdateCalls++;
        if (BlockGlobally)
            return null;

        return TypeUpdateResult ?? BatchExecutionResult.ForType([], 0);
    }
}
