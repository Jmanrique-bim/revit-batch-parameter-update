using System.ComponentModel;
using System.Runtime.CompilerServices;
using BatchParamUpdate.Domain.Model;

namespace BatchParamUpdate.UI.Wpf.ViewModels;

public sealed class SharedSearchViewModel : INotifyPropertyChanged
{
    public SharedSearchViewModel(SharedSearchQuery query) => Query = query;

    public SharedSearchQuery Query { get; }

    public string Text
    {
        get => Query.Text;
        set
        {
            if (Query.Text == value)
                return;
            Query.Text = value;
            OnPropertyChanged();
            TextChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? TextChanged;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
