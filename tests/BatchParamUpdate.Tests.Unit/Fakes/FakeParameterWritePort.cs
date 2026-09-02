using BatchParamUpdate.Domain.Model;
using BatchParamUpdate.Domain.Ports;

namespace BatchParamUpdate.Tests.Unit.Fakes;

public sealed class FakeParameterWritePort : IParameterWritePort
{
    public bool BlockGlobally { get; set; }

    public bool Revert { get; set; }

    public Exception? ThrowOnExecute { get; set; }

    public Dictionary<string, SkipReason> SkipsByElementId { get; } = new();

    public int ExecuteCalls { get; private set; }

    public List<BatchProgress> ProgressReports { get; } = [];

    public SelectionContext? LastScope { get; private set; }

    public ParameterCandidate? LastTargetParameter { get; private set; }

    public string? LastNewValue { get; private set; }

    public BatchExecutionResult? Execute(
        SelectionContext scope,
        ParameterCandidate targetParameter,
        string newValue,
        IProgress<BatchProgress> progress)
    {
        ExecuteCalls++;
        LastScope = scope;
        LastTargetParameter = targetParameter;
        LastNewValue = newValue;
        if (ThrowOnExecute is { } ex)
            throw ex;
        if (BlockGlobally)
            return null;

        var total = scope.ElementRefs.Count;
        progress.Report(new BatchProgress(0, total));
        for (var i = 0; i < total; i++)
        {
            ProgressReports.Add(new BatchProgress(i + 1, total));
            progress.Report(new BatchProgress(i + 1, total));
        }

        var skips = scope.ElementRefs
            .Where(e => SkipsByElementId.ContainsKey(e.Id))
            .Select(e => ElementSkip.Create(e, SkipsByElementId[e.Id]))
            .ToList();

        return Revert
            ? BatchExecutionResult.Reverted(skips)
            : BatchExecutionResult.Committed(total - skips.Count, skips);
    }
}
