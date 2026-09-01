using Autodesk.Revit.DB;
using BatchParamUpdate.Adapters.Revit.DialogSuppression;
using BatchParamUpdate.Domain.Model;
using BatchParamUpdate.Domain.Ports;

namespace BatchParamUpdate.Adapters.Revit.Writing;

public sealed class RevitParameterWritePort : IParameterWritePort
{
    private readonly Document _doc;
    private readonly INativeDialogSuppressionPort _dialogs;

    public RevitParameterWritePort(Document doc, INativeDialogSuppressionPort dialogs)
    {
        _doc = doc;
        _dialogs = dialogs;
    }

    public BatchExecutionResult? ExecuteInstanceUpdate(
        SelectionContext scope,
        ParameterCandidate targetParameter,
        string newValue)
    {
        using var suppress = _dialogs.SuppressNativeDialogsDuringBatch();
        using var tx = new Transaction(_doc, "Batch Parameter Update");
        ApplyFailureHandling(tx);
        if (tx.Start() != TransactionStatus.Started)
            return null;

        var skips = new List<ElementSkip>();
        var updated = 0;
        foreach (var eref in scope.ElementRefs)
        {
            var skip = TryWriteInstance(eref, targetParameter.Name, newValue);
            if (skip is null)
                updated++;
            else
                skips.Add(skip);
        }

        tx.Commit();
        return BatchExecutionResult.ForInstance(updated, skips);
    }

    public BatchExecutionResult? ExecuteTypeUpdate(
        SelectionContext scope,
        ParameterCandidate targetParameter,
        string newValue)
    {
        using var suppress = _dialogs.SuppressNativeDialogsDuringBatch();
        using var tx = new Transaction(_doc, "Batch Parameter Update");
        ApplyFailureHandling(tx);
        if (tx.Start() != TransactionStatus.Started)
            return null;

        var types = new Dictionary<string, ResolvedType>(StringComparer.Ordinal);
        var written = new HashSet<string>(StringComparer.Ordinal);
        foreach (var eref in scope.ElementRefs)
        {
            var element = GetElement(eref);
            if (element is null)
                continue;

            var typeId = element.GetTypeId();
            if (typeId == ElementId.InvalidElementId)
                continue;

            var type = _doc.GetElement(typeId);
            if (type is null)
                continue;

            var key = typeId.ToString();
            if (types.TryGetValue(key, out var existing))
            {
                types[key] = existing with
                {
                    SourceElementRefs = existing.SourceElementRefs.Append(eref).ToList()
                };
            }
            else
            {
                types[key] = new ResolvedType(key, type.Name, [eref]);
            }

            if (!written.Contains(key))
            {
                var param = FindTextParameter(type, targetParameter.Name);
                if (param is not null)
                {
                    param.Set(newValue);
                    written.Add(key);
                }
            }
        }

        tx.Commit();
        return BatchExecutionResult.ForType(types.Values.ToList(), CountElementsOfTypes(types.Keys));
    }

    private ElementSkip? TryWriteInstance(ElementRef eref, string parameterName, string newValue)
    {
        var element = GetElement(eref);
        if (element is null)
            return ElementSkip.Create(eref, SkipReason.ParameterMissing);

        if (element.GroupId != ElementId.InvalidElementId)
            return ElementSkip.Create(eref, SkipReason.ModelGroupMember);

        if (_dialogs.GetWorkshareStatus(eref) == WorkshareStatus.OwnedByOtherUser)
            return ElementSkip.Create(eref, SkipReason.WorksharingOwnedByOther);

        var param = FindParameter(element, parameterName);
        if (param is null)
            return ElementSkip.Create(eref, SkipReason.ParameterMissing);
        if (param.IsReadOnly)
            return ElementSkip.Create(eref, SkipReason.ParameterReadOnly);
        if (param.StorageType != StorageType.String)
            return ElementSkip.Create(eref, SkipReason.ParameterNotText);

        try
        {
            param.Set(newValue);
            return null;
        }
        catch (Autodesk.Revit.Exceptions.InvalidOperationException)
        {
            return ElementSkip.Create(eref, SkipReason.OtherSuppressedNativeDialog);
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
        => TryParseId(eref.Id, out var id) ? _doc.GetElement(id) : null;

    private static Parameter? FindParameter(Element element, string name)
        => element.LookupParameter(name) ?? FindByDefinitionName(element, name);

    private static Parameter? FindTextParameter(Element element, string name)
    {
        var param = FindParameter(element, name);
        return param is { StorageType: StorageType.String, IsReadOnly: false } ? param : null;
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

    // ponytail: full-model scan for Type-path updated count. Ceiling: large models; upgrade to a category-scoped collector.
    private int CountElementsOfTypes(IEnumerable<string> typeIds)
    {
        var ids = new HashSet<string>(typeIds, StringComparer.Ordinal);
        var count = 0;
        foreach (var element in new FilteredElementCollector(_doc).WhereElementIsNotElementType())
        {
            if (ids.Contains(element.GetTypeId().ToString()))
                count++;
        }

        return count;
    }

    private static bool TryParseId(string id, out ElementId elementId)
    {
        if (long.TryParse(id, out var value))
        {
            elementId = new ElementId(value);
            return true;
        }

        elementId = ElementId.InvalidElementId;
        return false;
    }
}
