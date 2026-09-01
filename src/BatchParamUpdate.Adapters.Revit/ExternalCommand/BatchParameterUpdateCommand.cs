using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace BatchParamUpdate.Adapters.Revit.ExternalCommand;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public sealed class BatchParameterUpdateCommand : IExternalCommand
{
    // ponytail: T097 (US4) owns the selection → discovery → execute wiring.
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        => Result.Succeeded;
}
