using System.ComponentModel;
using System.Runtime.CompilerServices;
using BatchParamUpdate.Domain.Model;

namespace BatchParamUpdate.UI.Wpf.ViewModels;

public sealed class BatchExecutionViewModel : INotifyPropertyChanged
{
    private bool _isExecuting;
    private int _done;
    private int _total;

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

    public int Done
    {
        get => _done;
        private set { _done = value; OnPropertyChanged(); }
    }

    public int Total
    {
        get => _total;
        private set { _total = value; OnPropertyChanged(); }
    }

    public void Report(BatchProgress progress)
    {
        Total = progress.Total;
        Done = progress.Done;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
