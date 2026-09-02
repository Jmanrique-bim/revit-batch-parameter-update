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

    public ReplacementValueViewModel(
        BatchUpdateCoordinator coordinator,
        BatchExecutionViewModel execution,
        BatchSummaryViewModel summary)
    {
        _coordinator = coordinator;
        _execution = execution;
        _summary = summary;
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
        && _coordinator.Step == SessionState.AwaitingReplacementValue;

    public ICommand RunCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void NotifyCanRun()
    {
        OnPropertyChanged(nameof(CanRun));
        (RunCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    private void Run()
    {
        _execution.IsExecuting = true;
        try
        {
            var progress = new DispatcherPumpProgress(_execution.Report);
            var result = _coordinator.Run(progress);
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
