using BatchParamUpdate.Application.Workflow;
using BatchParamUpdate.Domain.Model;
using Xunit;

namespace BatchParamUpdate.Tests.Unit.Architecture;

/// <summary>
/// Locks the hexagon: business layers must not depend outward. If someone wires UI or a Revit
/// adapter into Domain/Application again, this fails.
/// </summary>
public sealed class LayerDependencyTests
{
    private static readonly string[] Forbidden =
    [
        "BatchParamUpdate.UI.Wpf",
        "BatchParamUpdate.Adapters.Revit",
        "BatchParamUpdate.Adapters.Persistence",
        "BatchParamUpdate.Installer"
    ];

    [Fact]
    public void Application_DoesNotReferenceUiOrAdapters()
    {
        var referenced = typeof(BatchUpdateCoordinator).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name)
            .ToArray();

        Assert.DoesNotContain(referenced, name => Array.IndexOf(Forbidden, name) >= 0);
    }

    [Fact]
    public void Domain_DoesNotReferenceAnyOtherProjectLayer()
    {
        var referenced = typeof(SelectionContext).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name)
            .Where(n => n is not null && n.StartsWith("BatchParamUpdate", StringComparison.Ordinal))
            .ToArray();

        Assert.Empty(referenced);
    }
}
