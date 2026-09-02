using BatchParamUpdate.Application.Workflow;
using BatchParamUpdate.Domain.ErrorCatalog;
using BatchParamUpdate.Domain.Model;
using BatchParamUpdate.Tests.Unit.Fakes;
using Xunit;

namespace BatchParamUpdate.Tests.Unit.Application;

public sealed class BatchUpdateCoordinatorTests
{
    [Fact]
    public void EstablishSelection_WithNoPreExisting_ReportsNeedsManualPick_AndKeepsScopeEmpty()
    {
        var h = new CoordinatorHarness();

        var result = h.Coordinator.EstablishSelection();

        Assert.IsType<SelectionResult.NeedsManualPick>(result);
        Assert.False(h.Coordinator.State.HasScope);
        Assert.Equal(SessionState.Started, h.Coordinator.Step);
    }

    [Fact]
    public void ManualPick_AfterEmptyLaunch_AdoptsScopeAndRunsDiscovery()
    {
        var h = new CoordinatorHarness();
        h.WithDiscovered("Comments");
        h.Coordinator.EstablishSelection();

        h.Coordinator.AdoptManualSelection(
            new SelectionContext([new ElementRef("7", "Walls")], SelectionOrigin.ManualPick));

        Assert.True(h.Coordinator.State.HasScope);
        Assert.Equal(SessionState.Discovering, h.Coordinator.Step);
        Assert.Contains(h.Coordinator.Candidates.Candidates, c => c.Name == "Comments");
    }

    [Fact]
    public void ChooseParameter_WithEmptyScope_BlocksWithEmptySelectionError()
    {
        var h = new CoordinatorHarness();
        h.Coordinator.EstablishSelection();

        var ok = h.Coordinator.ChooseParameter(new ParameterCandidate("Comments", []));

        Assert.False(ok);
        Assert.Equal(ErrorCode.EmptySelection, h.Coordinator.LastError);
    }

    [Fact]
    public void Run_WithEmptyScope_BlocksWithEmptySelectionError()
    {
        var h = new CoordinatorHarness();
        h.Coordinator.EstablishSelection();

        var result = h.Coordinator.Run();

        Assert.Null(result);
        Assert.Equal(ErrorCode.EmptySelection, h.Coordinator.LastError);
    }

    [Fact]
    public void PreparedRun_IsImmuneToStateMutationBeforeTheWrite()
    {
        var h = new CoordinatorHarness();
        h.WithDiscovered("Comments");
        h.WithPreExisting(new ElementRef("1", "Walls"));
        h.Coordinator.EstablishSelection();
        h.Coordinator.ChooseParameter(h.Coordinator.Candidates.Candidates[0]);
        h.Coordinator.SetValue("confirmed");

        var operation = h.Coordinator.PrepareRun();
        Assert.NotNull(operation);

        // The modeless window keeps the inputs live until the deferred write runs.
        h.Coordinator.SetValue("changed-after-click");
        h.Coordinator.AdoptManualSelection(
            new SelectionContext([new ElementRef("99", "Doors")], SelectionOrigin.ManualPick));

        h.Coordinator.Run(operation!, new Progress<BatchProgress>());

        Assert.Equal("confirmed", h.Write.LastNewValue);
        Assert.Equal(["1"], h.Write.LastScope!.ElementRefs.Select(e => e.Id));
    }

    [Fact]
    public void Run_WhenWriteThrows_CopiesErrorRaisesBlockAndChanged()
    {
        var h = new CoordinatorHarness();
        h.WithDiscovered("Comments");
        h.WithPreExisting(new ElementRef("1", "Walls"));
        h.Write.ThrowOnExecute = new InvalidOperationException("Revit said no");
        h.Coordinator.EstablishSelection();
        h.Coordinator.ChooseParameter(h.Coordinator.Candidates.Candidates[0]);
        h.Coordinator.SetValue("v");

        var changed = 0;
        h.Coordinator.Changed += () => changed++;

        var thrown = Record.Exception(() => h.Coordinator.Run());

        Assert.Null(thrown);
        Assert.Null(h.Coordinator.LastResult);
        Assert.Equal(ErrorCode.DocumentNotModifiable, h.Coordinator.LastError);
        Assert.Equal(SessionState.Blocked, h.Coordinator.Step);
        Assert.True(changed > 0);
        Assert.Contains(h.Logger.Lines, l => l.Contains("run\tstart", StringComparison.Ordinal));
        Assert.Contains(h.Logger.Lines, l => l.Contains("to=Blocked", StringComparison.Ordinal));
        Assert.Contains(h.Logger.Lines, l => l.StartsWith("ERROR DocumentNotModifiable", StringComparison.Ordinal));
    }

    [Fact]
    public void Run_WhenTransactionReverts_SurfacesRolledBackResult()
    {
        var h = new CoordinatorHarness();
        h.WithDiscovered("Comments");
        h.WithPreExisting(new ElementRef("1", "Walls"));
        h.Write.Revert = true;
        h.Coordinator.EstablishSelection();
        h.Coordinator.ChooseParameter(h.Coordinator.Candidates.Candidates[0]);
        h.Coordinator.SetValue("v");

        var result = h.Coordinator.Run();

        Assert.NotNull(result);
        Assert.True(result!.RolledBack);
        Assert.Equal(ErrorCode.BatchRolledBack, h.Coordinator.LastError);
        Assert.Equal(SessionState.Blocked, h.Coordinator.Step);
    }

    [Fact]
    public void Run_ReportsProgressFromZeroToTotal()
    {
        var h = new CoordinatorHarness();
        h.WithDiscovered("Comments");
        h.WithPreExisting(new ElementRef("1", "Walls"), new ElementRef("2", "Walls"), new ElementRef("3", "Walls"));
        h.Coordinator.EstablishSelection();
        h.Coordinator.ChooseParameter(h.Coordinator.Candidates.Candidates[0]);
        h.Coordinator.SetValue("v");

        var reports = new List<BatchProgress>();
        h.Coordinator.Run(new Progress<BatchProgress>(reports.Add));
        // Progress<T> posts asynchronously; the fake also records synchronously.
        var recorded = h.Write.ProgressReports;

        Assert.Equal(new BatchProgress(3, 3), recorded[^1]);
        Assert.Equal(3, recorded.Count);
    }

    [Fact]
    public void Complete_AfterCancelledFlow_MovesSessionToCancelled()
    {
        var h = new CoordinatorHarness();
        h.Coordinator.EstablishSelection();

        h.Coordinator.Complete();

        Assert.Equal(SessionState.Cancelled, h.Coordinator.Step);
    }
}
