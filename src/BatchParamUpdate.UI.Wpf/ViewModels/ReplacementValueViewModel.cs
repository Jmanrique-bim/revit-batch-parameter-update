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
    private readonly RecordSessionUseCase? _record;
    private string _newValue = "";

    public ReplacementValueViewModel(
        Func<ReplacementOperation?> operation,
        Func<SelectionContext> scope,
        Session session,
        RunBatchUpdateUseCase run,
        BatchExecutionViewModel execution,
        BatchSummaryViewModel summary,
        RecordSessionUseCase? record = null)
    {
        _operation = operation;
        _scope = scope;
        _session = session;
        _run = run;
        _execution = execution;
        _summary = summary;
        _record = record;
        RunCommand = new RelayCommand(Run, () => CanRun);
        LogGate("init");
    }

    public void NotifyCanRun(string requery = "raised")
    {
        OnPropertyChanged(nameof(CanRun));
        (RunCommand as RelayCommand)?.RaiseCanExecuteChanged();
        LogGate(requery);
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
            NotifyCanRun("value");
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
            _record?.Trace(
                "ui",
                "run",
                "blocked",
                ("reason", operation is null ? "noOperation" : "emptyValue"),
                ("session", _session.State),
                ("canRun", CanRun));
            OnPropertyChanged(nameof(ValidationMessage));
            LogGate("blocked");
            return;
        }

        _record?.Trace(
            "ui",
            "run",
            "click",
            ("session", _session.State),
            ("binding", operation.TargetParameter.Binding),
            ("name", operation.TargetParameter.Name),
            ("scope", _scope().ElementRefs.Count));
        _execution.IsExecuting = true;
        try
        {
            var result = _run.Execute(_session, operation, _scope());
            _summary.Show(result, _run.Error);
            _record?.Trace(
                "ui",
                "summary",
                "shown",
                ("ok", _run.Error is null && result is not null),
                ("skips", _summary.HasSkips),
                ("session", _session.State));
        }
        finally
        {
            _execution.IsExecuting = false;
            NotifyCanRun("done");
        }
    }

    private void LogGate(string requery)
    {
        var command = RunCommand as RelayCommand;
        _record?.TraceGate(
            ("enabled", CanRun),
            ("session", _session.State),
            ("hasValue", !string.IsNullOrWhiteSpace(NewValue)),
            ("hasOperation", _operation() is not null),
            ("requery", requery),
            ("subscribers", command?.ListenerCount ?? 0));
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
