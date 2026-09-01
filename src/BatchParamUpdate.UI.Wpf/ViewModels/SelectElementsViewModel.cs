using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using BatchParamUpdate.Domain.Model;
using BatchParamUpdate.Domain.Ports;

namespace BatchParamUpdate.UI.Wpf.ViewModels;

public sealed class SelectElementsViewModel : INotifyPropertyChanged
{
    private readonly IElementSelectionPort? _selection;
    private readonly Session? _session;
    private readonly Action? _beforePick;
    private readonly Action? _afterPick;
    private SelectionContext _context;

    public SelectElementsViewModel(SelectionContext selection)
        : this(selection, selectionPort: null, session: null)
    {
    }

    public SelectElementsViewModel(
        SelectionContext selection,
        IElementSelectionPort? selectionPort,
        Session? session,
        Action? beforePick = null,
        Action? afterPick = null)
    {
        _context = selection;
        _selection = selectionPort;
        _session = session;
        _beforePick = beforePick;
        _afterPick = afterPick;
        SelectElementsCommand = new RelayCommand(PickManually, () => IsSelectElementsEnabled);
    }

    public ICommand SelectElementsCommand { get; }

    public bool IsSelectElementsEnabled => _context.Origin == SelectionOrigin.ManualPick;

    public bool HasNoElementsInScope => !_context.IsValid;

    public SelectionContext Selection => _context;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void PickManually()
    {
        if (_selection is null)
            return;

        _beforePick?.Invoke();
        SelectionContext? picked;
        try
        {
            picked = _selection.PromptManualSelection();
        }
        finally
        {
            _afterPick?.Invoke();
        }

        if (picked is not { IsValid: true })
            return;

        _context = picked;
        if (_session is { State: SessionState.Started })
            _session.TransitionTo(SessionState.Discovering);

        OnPropertyChanged(nameof(Selection));
        OnPropertyChanged(nameof(HasNoElementsInScope));
        OnPropertyChanged(nameof(IsSelectElementsEnabled));
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
