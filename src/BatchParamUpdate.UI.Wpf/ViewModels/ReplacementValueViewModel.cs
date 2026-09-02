using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using BatchParamUpdate.Application.Workflow;
using BatchParamUpdate.Domain.ErrorCatalog;
using BatchParamUpdate.Domain.Model;

namespace BatchParamUpdate.UI.Wpf.ViewModels;

public sealed class ReplacementValueViewModel : INotifyPropertyChanged
{
    private readonly BatchUpdateCoordinator _coordinator;
    private readonly BatchExecutionViewModel _execution;
    private readonly BatchSummaryViewModel _summary;
    private readonly Func<Action, Task> _runOnRevit;

    public ReplacementValueViewModel(
        BatchUpdateCoordinator coordinator,
        BatchExecutionViewModel execution,
        BatchSummaryViewModel summary,
        Func<Action, Task>? runOnRevit = null)
    {
        _coordinator = coordinator;
        _execution = execution;
        _summary = summary;
        // The Revit host swaps in an ExternalEvent bridge (modeless window can't open a
        // Transaction directly). Default runs inline for non-Revit hosts and tests.
        _runOnRevit = runOnRevit ?? (work => { work(); return Task.CompletedTask; });
        RunCommand = new RelayCommand(Run, () => CanRun);
    }

    public string NewValue
    {
        get => _coordinator.State.NewValue;
        set
        {
            if (_coordinator.State.NewValue == value)
                return;
            _coordinator.SetValue(value);
            OnPropertyChanged();
            OnPropertyChanged(nameof(ValidationMessage));
            NotifyCanRun();
        }
    }

    public string? ValidationMessage => string.IsNullOrWhiteSpace(NewValue)
        ? ErrorWarningCatalog.Message(ErrorCode.EmptyValue)
        : null;

    public bool CanRun =>
        _coordinator.State.Target is not null
        && !string.IsNullOrWhiteSpace(NewValue)
        && _coordinator.Step == SessionState.AwaitingReplacementValue
        && !_execution.IsExecuting;

    public ICommand RunCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void NotifyCanRun()
    {
        OnPropertyChanged(nameof(CanRun));
        (RunCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    private async void Run()
    {
        if (_execution.IsExecuting)
            return;

        _execution.IsExecuting = true;
        NotifyCanRun();
        try
        {
            // The write loop runs on the Revit API thread (== this UI thread) inside the bridge,
            // so Report is a direct call; RenderPumpProgress forces a repaint per element without
            // draining input.
            var progress = new RenderPumpProgress(_execution.Report);
            BatchExecutionResult? result = null;
            await _runOnRevit(() => result = _coordinator.Run(progress));
            _summary.Show(result, _coordinator.LastError);
        }
        finally
        {
            _execution.IsExecuting = false;
            NotifyCanRun();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
