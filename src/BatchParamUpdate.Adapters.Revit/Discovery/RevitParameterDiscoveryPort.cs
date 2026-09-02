using Autodesk.Revit.DB;
using BatchParamUpdate.Domain.Model;
using BatchParamUpdate.Domain.Ports;

namespace BatchParamUpdate.Adapters.Revit.Discovery;

public sealed class RevitParameterDiscoveryPort : IParameterDiscoveryPort
{
    private readonly Document _doc;

    public RevitParameterDiscoveryPort(Document doc) => _doc = doc;

    public ParameterCandidateSet Discover(SelectionContext scope)
        => new(Collect(scope));

    private IEnumerable<ParameterCandidate> Collect(SelectionContext scope)
    {
        foreach (var eref in scope.ElementRefs)
        {
            var element = ResolveElement(eref);
            if (element is null)
                continue;

            foreach (Parameter parameter in element.Parameters)
            {
                if (parameter.StorageType != StorageType.String || parameter.IsReadOnly)
                    continue;

                var name = parameter.Definition?.Name;
                if (string.IsNullOrEmpty(name))
                    continue;

                yield return new ParameterCandidate(
                    name,
                    [eref],
                    [parameter.AsString() ?? ""],
                    KeyFor(parameter, name));
            }
        }
    }

    private Element? ResolveElement(ElementRef eref)
        => long.TryParse(eref.Id, out var idValue) ? _doc.GetElement(new ElementId(idValue)) : null;

    private static ParameterKey KeyFor(Parameter parameter, string name)
    {
        int? builtIn = parameter.Definition is InternalDefinition def
                       && def.BuiltInParameter != BuiltInParameter.INVALID
            ? (int)def.BuiltInParameter
            : null;
        Guid? sharedGuid = parameter.IsShared ? parameter.GUID : null;
        return new ParameterKey(builtIn, sharedGuid, name);
    }
}
