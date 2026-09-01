using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BatchParamUpdate.UI.Wpf.ViewModels;

public sealed class BatchExecutionViewModel : INotifyPropertyChanged
{
    private bool _isExecuting;

    public bool IsExecuting
    {
        get => _isExecuting;
        set
        {
            if (_isExecuting == value)
                return;
            _isExecuting = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
