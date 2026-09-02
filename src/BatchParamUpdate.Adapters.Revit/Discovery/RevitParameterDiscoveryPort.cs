using Autodesk.Revit.DB;
using BatchParamUpdate.Domain.Model;
using BatchParamUpdate.Domain.Ports;

namespace BatchParamUpdate.Adapters.Revit.Discovery;

public sealed class RevitParameterDiscoveryPort : IParameterDiscoveryPort
{
    private readonly Document _doc;

    public RevitParameterDiscoveryPort(Document doc) => _doc = doc;

    public InstanceParameterCandidateSet DiscoverInstanceCandidates(SelectionContext scope)
        => new(Collect(scope, fromType: false));

    public TypeParameterCandidateSet DiscoverTypeCandidates(SelectionContext scope)
        => new(Collect(scope, fromType: true));

    private IEnumerable<ParameterCandidate> Collect(SelectionContext scope, bool fromType)
    {
        var binding = fromType ? ParameterBinding.Type : ParameterBinding.Instance;
        foreach (var eref in scope.ElementRefs)
        {
            var host = ResolveHost(eref, fromType);
            if (host is null)
                continue;

            foreach (Parameter parameter in host.Parameters)
            {
                if (parameter.StorageType != StorageType.String || parameter.IsReadOnly)
                    continue;

                var name = parameter.Definition?.Name;
                if (string.IsNullOrEmpty(name))
                    continue;

                yield return new ParameterCandidate(
                    name,
                    binding,
                    [eref],
                    [parameter.AsString() ?? ""]);
            }
        }
    }

    private Element? ResolveHost(ElementRef eref, bool fromType)
    {
        if (!long.TryParse(eref.Id, out var idValue))
            return null;

        var element = _doc.GetElement(new ElementId(idValue));
        if (element is null)
            return null;

        if (!fromType)
            return element;

        var typeId = element.GetTypeId();
        return typeId == ElementId.InvalidElementId ? null : _doc.GetElement(typeId);
    }
}
