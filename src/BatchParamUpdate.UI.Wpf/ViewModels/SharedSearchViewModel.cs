using System.ComponentModel;
using System.Runtime.CompilerServices;
using BatchParamUpdate.Domain.Model;

namespace BatchParamUpdate.UI.Wpf.ViewModels;

public sealed class SharedSearchViewModel : INotifyPropertyChanged
{
    public SharedSearchViewModel(ParameterSearch query) => Query = query;

    public ParameterSearch Query { get; private set; }

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

    public void ReplaceSet(ParameterCandidateSet set)
    {
        Query = new ParameterSearch(set, Query.Text);
        OnPropertyChanged(nameof(Text));
        TextChanged?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? TextChanged;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
