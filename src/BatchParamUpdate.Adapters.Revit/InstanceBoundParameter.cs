using Autodesk.Revit.DB;

namespace BatchParamUpdate.Adapters.Revit;

/// <summary>
/// Type-bound parameters appear on <see cref="Element.Parameters"/> of instances; writing them
/// changes every instance of that type. This add-in is instance-only.
/// </summary>
internal static class InstanceBoundParameter
{
    public static bool IsInstance(Parameter parameter)
    {
        var definition = parameter.Definition;
        if (definition is null)
            return false;

        var doc = parameter.Element.Document;
        var bindings = doc.ParameterBindings;
        if (bindings.Contains(definition) && bindings.get_Item(definition) is TypeBinding)
            return false;

        if (parameter.Element is ElementType)
            return false;

        var typeId = parameter.Element.GetTypeId();
        if (typeId == ElementId.InvalidElementId)
            return true;

        var type = doc.GetElement(typeId);
        if (type is null)
            return true;

        var onType = SameDefinition(type, parameter);
        return onType is null || parameter.Id != onType.Id;
    }

    private static Parameter? SameDefinition(Element type, Parameter parameter)
    {
        if (parameter.IsShared)
            return type.get_Parameter(parameter.GUID);

        if (parameter.Definition is InternalDefinition def
            && def.BuiltInParameter != BuiltInParameter.INVALID)
            return type.get_Parameter(def.BuiltInParameter);

        var name = parameter.Definition?.Name;
        return string.IsNullOrEmpty(name) ? null : type.LookupParameter(name);
    }
}
