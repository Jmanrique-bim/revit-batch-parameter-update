using System.Windows.Input;

namespace BatchParamUpdate.UI.Wpf.ViewModels;

internal sealed class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => _execute();

    public event EventHandler? CanExecuteChanged
    {
        add
        {
            CommandManager.RequerySuggested += value;
            _canExecuteChanged += value;
        }
        remove
        {
            CommandManager.RequerySuggested -= value;
            _canExecuteChanged -= value;
        }
    }

    // ponytail: CommandManager dies after Revit PickObjects; Raise is the host workaround.
    public void RaiseCanExecuteChanged() => _canExecuteChanged?.Invoke(this, EventArgs.Empty);

    public int ListenerCount => _canExecuteChanged?.GetInvocationList().Length ?? 0;

    private event EventHandler? _canExecuteChanged;
}
