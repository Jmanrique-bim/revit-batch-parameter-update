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
        Assert.Contains(h.Logger.Lines, l => l.Contains("why=empty-scope", StringComparison.Ordinal));
    }

    [Fact]
    public void AdoptManualSelection_AfterChoose_ClearsTargetAndReturnsToDiscovering()
    {
        var h = new CoordinatorHarness();
        h.WithDiscovered("Comments");
        h.WithPreExisting(new ElementRef("1", "Walls"));
        h.Coordinator.EstablishSelection();
        h.Coordinator.ChooseParameter(h.Coordinator.Candidates.Candidates[0]);
        h.Coordinator.SetValue("v");

        h.Coordinator.AdoptManualSelection(
            new SelectionContext([new ElementRef("7", "Walls")], SelectionOrigin.ManualPick));

        Assert.Null(h.Coordinator.State.Target);
        Assert.Equal(SessionState.Discovering, h.Coordinator.Step);
        Assert.Null(h.Coordinator.Run());
        Assert.Equal(ErrorCode.NoParameterSelected, h.Coordinator.LastError);
    }

    [Fact]
    public void Run_WhenProgressReentersRunOrPick_DoesNotNestExecute()
    {
        var h = new CoordinatorHarness();
        h.WithDiscovered("Comments");
        h.WithPreExisting(new ElementRef("1", "Walls"));
        h.Coordinator.EstablishSelection();
        h.Coordinator.ChooseParameter(h.Coordinator.Candidates.Candidates[0]);
        h.Coordinator.SetValue("v");

        h.Coordinator.Run(new ImmediateProgress(_ =>
        {
            h.Coordinator.Run();
            h.Coordinator.AdoptManualSelection(
                new SelectionContext([new ElementRef("99", "Walls")], SelectionOrigin.ManualPick));
        }));

        Assert.Equal(1, h.Write.ExecuteCalls);
        Assert.Equal("1", h.Coordinator.State.Scope.ElementRefs[0].Id);
        Assert.Equal(SessionState.AwaitingReplacementValue, h.Coordinator.Step);
    }

    [Fact]
    public void Complete_AfterCommittedBatchThenRepick_StaysCompleted()
    {
        var h = new CoordinatorHarness();
        h.WithDiscovered("Comments");
        h.WithPreExisting(new ElementRef("1", "Walls"));
        h.Coordinator.EstablishSelection();
        h.Coordinator.ChooseParameter(h.Coordinator.Candidates.Candidates[0]);
        h.Coordinator.SetValue("v");
        h.Coordinator.Run();

        h.Coordinator.AdoptManualSelection(
            new SelectionContext([new ElementRef("7", "Walls")], SelectionOrigin.ManualPick));
        h.Coordinator.Complete();

        Assert.Equal(SessionState.Completed, h.Coordinator.Step);
        Assert.Contains(
            h.Logger.Lines,
            l => l.Contains("why=batch-ran", StringComparison.Ordinal)
                 && l.Contains("session=Completed", StringComparison.Ordinal));
    }

    [Fact]
    public void Complete_WhenReadyButNeverRan_LogsCanRunNeverClicked()
    {
        var h = new CoordinatorHarness();
        h.WithDiscovered("Mark");
        h.WithPreExisting(new ElementRef("1", "Walls"));
        h.Coordinator.EstablishSelection();
        h.Coordinator.ChooseParameter(h.Coordinator.Candidates.Candidates[0]);
        h.Coordinator.SetValue("test 1");

        h.Coordinator.Complete();

        Assert.Equal(SessionState.Cancelled, h.Coordinator.Step);
        Assert.Contains(
            h.Logger.Lines,
            l => l.Contains("why=can-run-never-clicked", StringComparison.Ordinal)
                 && l.Contains("canRun=true", StringComparison.Ordinal)
                 && l.Contains("param=Mark", StringComparison.Ordinal));
    }

    // Progress<T> posts to SynchronizationContext; the write path reports on the same
    // stack, so the re-entry guard has to be exercised with a synchronous IProgress.
    private sealed class ImmediateProgress(Action<BatchProgress> onReport) : IProgress<BatchProgress>
    {
        public void Report(BatchProgress value) => onReport(value);
    }
}
