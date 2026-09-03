using BatchParamUpdate.Domain.Model;

namespace BatchParamUpdate.Application.Workflow;

/// <summary>
/// Outcome of resolving the element scope at add-in launch.
/// </summary>
public abstract record SelectionResult
{
    /// <summary>A valid pre-existing selection was adopted (User Story 1).</summary>
    public sealed record Established(SelectionContext Scope) : SelectionResult;

    /// <summary>No pre-existing selection; the window opens with manual pick enabled (User Story 2).</summary>
    public sealed record NeedsManualPick : SelectionResult;
}
