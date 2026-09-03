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
        // CanExecute stays true. After Revit 2026 PickObjects (Finish/Cancel),
        // CommandManager is dead and Button.IsEnabledCore ANDs a stale CanExecute cache,
        // so gating Run here keeps the button off even when CanRun is true. IsEnabled={CanRun}
        // is the only gate; CommandManager.InvalidateRequerySuggested is the upgrade if a
        // future host keeps RequerySuggested alive across pick.
        RunCommand = new RelayCommand(Run);
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

    public bool HasCurrentValueSummary => _coordinator.State.Target is not null;

    public string CurrentValueSummary
    {
        get
        {
            var target = _coordinator.State.Target;
            if (target is null)
                return "";

            var values = target.ObservedValues
                .Select(v => string.IsNullOrEmpty(v) ? "(empty)" : v)
                .Distinct(StringComparer.Ordinal)
                .ToList();
            if (values.Count == 0)
                return $"No current value for {target.Name}.";
            if (values.Count == 1)
                return $"Current value of {target.Name}: {values[0]}";
            return $"Current values of {target.Name}: {string.Join(", ", values)}";
        }
    }

    public ICommand RunCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void NotifyCanRun()
    {
        OnPropertyChanged(nameof(CanRun));
        OnPropertyChanged(nameof(HasCurrentValueSummary));
        OnPropertyChanged(nameof(CurrentValueSummary));
    }

    private void Run()
    {
        if (!CanRun)
            return;

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
