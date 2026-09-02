using Autodesk.Revit.DB;
using BatchParamUpdate.Adapters.Revit.DialogSuppression;
using BatchParamUpdate.Domain.Model;
using BatchParamUpdate.Domain.Ports;

namespace BatchParamUpdate.Adapters.Revit.Writing;

public sealed class RevitParameterWritePort : IParameterWritePort
{
    private readonly Document _doc;
    private readonly INativeDialogSuppressionPort _dialogs;
    private readonly IWorksharingStatusPort _worksharing;

    public RevitParameterWritePort(
        Document doc,
        INativeDialogSuppressionPort dialogs,
        IWorksharingStatusPort worksharing)
    {
        _doc = doc;
        _dialogs = dialogs;
        _worksharing = worksharing;
    }

    public BatchExecutionResult? Execute(
        SelectionContext scope,
        ParameterCandidate targetParameter,
        string newValue,
        IProgress<BatchProgress> progress)
    {
        using var suppress = _dialogs.SuppressNativeDialogsDuringBatch();
        using var tx = new Transaction(_doc, "Batch Parameter Update");
        ApplyFailureHandling(tx);
        if (tx.Start() != TransactionStatus.Started)
            return null;

        var key = targetParameter.ResolvedKey;
        var total = scope.ElementRefs.Count;
        var skips = new List<ElementSkip>();
        var updated = 0;
        progress.Report(new BatchProgress(0, total));
        for (var i = 0; i < total; i++)
        {
            var skip = TryWriteInstance(scope.ElementRefs[i], key, newValue);
            if (skip is null)
                updated++;
            else
                skips.Add(skip);
            progress.Report(new BatchProgress(i + 1, total));
        }

        return tx.Commit() == TransactionStatus.Committed
            ? BatchExecutionResult.Committed(updated, skips)
            : BatchExecutionResult.Reverted(skips);
    }

    private ElementSkip? TryWriteInstance(ElementRef eref, ParameterKey key, string newValue)
    {
        var element = GetElement(eref);
        var param = element is null ? null : FindParameter(element, key);

        var state = new ParameterState(
            ElementFound: element is not null,
            InModelGroup: element is not null && element.GroupId != ElementId.InvalidElementId,
            Workshare: _worksharing.GetWorkshareStatus(eref),
            ParameterFound: param is not null,
            IsReadOnly: param?.IsReadOnly ?? false,
            Storage: param is null
                ? ParameterStorageKind.None
                : param.StorageType == StorageType.String
                    ? ParameterStorageKind.Text
                    : ParameterStorageKind.NonText);

        var outcome = ParameterWriteDecision.Evaluate(state, () => TrySet(param!, newValue));
        return outcome is WriteOutcome.Skip skip ? ElementSkip.Create(eref, skip.Reason) : null;
    }

    private static bool TrySet(Parameter param, string value)
    {
        try
        {
            return param.Set(value);
        }
        catch (Autodesk.Revit.Exceptions.InvalidOperationException)
        {
            return false;
        }
    }

    private static void ApplyFailureHandling(Transaction tx)
    {
        var options = tx.GetFailureHandlingOptions();
        options.SetFailuresPreprocessor(RevitDialogSuppressionPort.CreateFailuresPreprocessor());
        options.SetClearAfterRollback(true);
        tx.SetFailureHandlingOptions(options);
    }

    private Element? GetElement(ElementRef eref)
        => long.TryParse(eref.Id, out var value) ? _doc.GetElement(new ElementId(value)) : null;

    private static Parameter? FindParameter(Element element, ParameterKey key)
    {
        if (key.BuiltInId is { } builtIn)
        {
            var byBuiltIn = element.get_Parameter((BuiltInParameter)builtIn);
            if (byBuiltIn is not null)
                return byBuiltIn;
        }

        if (key.SharedGuid is { } guid)
        {
            var byGuid = element.get_Parameter(guid);
            if (byGuid is not null)
                return byGuid;
        }

        return element.LookupParameter(key.Name) ?? FindByDefinitionName(element, key.Name);
    }

    private static Parameter? FindByDefinitionName(Element element, string name)
    {
        foreach (Parameter parameter in element.Parameters)
        {
            if (string.Equals(parameter.Definition?.Name, name, StringComparison.OrdinalIgnoreCase))
                return parameter;
        }

        return null;
    }
}
