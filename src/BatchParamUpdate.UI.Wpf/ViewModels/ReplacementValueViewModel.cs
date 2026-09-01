using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using BatchParamUpdate.Application.UseCases;
using BatchParamUpdate.Domain.ErrorCatalog;
using BatchParamUpdate.Domain.Model;

namespace BatchParamUpdate.UI.Wpf.ViewModels;

public sealed class ReplacementValueViewModel : INotifyPropertyChanged
{
    private readonly Func<ReplacementOperation?> _operation;
    private readonly Func<SelectionContext> _scope;
    private readonly Session _session;
    private readonly RunBatchUpdateUseCase _run;
    private readonly BatchExecutionViewModel _execution;
    private readonly BatchSummaryViewModel _summary;
    private string _newValue = "";

    public ReplacementValueViewModel(
        Func<ReplacementOperation?> operation,
        Func<SelectionContext> scope,
        Session session,
        RunBatchUpdateUseCase run,
        BatchExecutionViewModel execution,
        BatchSummaryViewModel summary)
    {
        _operation = operation;
        _scope = scope;
        _session = session;
        _run = run;
        _execution = execution;
        _summary = summary;
        RunCommand = new RelayCommand(Run, () => CanRun);
    }

    public string NewValue
    {
        get => _newValue;
        set
        {
            if (_newValue == value)
                return;
            _newValue = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ValidationMessage));
            OnPropertyChanged(nameof(CanRun));
        }
    }

    public string? ValidationMessage => string.IsNullOrWhiteSpace(NewValue)
        ? ErrorWarningCatalog.Message(ErrorCode.EmptyValue)
        : null;

    public bool CanRun =>
        !string.IsNullOrWhiteSpace(NewValue)
        && _session.State == SessionState.AwaitingReplacementValue;

    public ICommand RunCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Run()
    {
        var operation = _operation()?.WithNewValue(NewValue);
        if (operation is null || !operation.HasReplacementValue)
        {
            OnPropertyChanged(nameof(ValidationMessage));
            return;
        }

        _execution.IsExecuting = true;
        try
        {
            var result = _run.Execute(_session, operation, _scope());
            _summary.Show(result, _run.Error);
        }
        finally
        {
            _execution.IsExecuting = false;
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
