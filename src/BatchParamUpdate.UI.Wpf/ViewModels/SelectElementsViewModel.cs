using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using BatchParamUpdate.Application.UseCases;
using BatchParamUpdate.Domain.Model;
using BatchParamUpdate.Domain.Ports;

namespace BatchParamUpdate.UI.Wpf.ViewModels;

public sealed class SelectElementsViewModel : INotifyPropertyChanged
{
    private readonly IElementSelectionPort? _selection;
    private readonly Session? _session;
    private readonly RecordSessionUseCase? _record;
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
        Action? afterPick = null,
        RecordSessionUseCase? record = null)
    {
        _context = selection;
        _selection = selectionPort;
        _session = session;
        _record = record;
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

        _record?.Trace(
            "ui",
            "select",
            "pick.start",
            ("session", _session?.State),
            ("enabled", IsSelectElementsEnabled));
        _beforePick?.Invoke();
        _record?.Trace("ui", "window", "hide", ("cause", "pick"));
        SelectionContext? picked;
        try
        {
            picked = _selection.PromptManualSelection();
        }
        finally
        {
            _afterPick?.Invoke();
            _record?.Trace("ui", "window", "show", ("cause", "pick"));
        }

        if (picked is not { IsValid: true })
        {
            _record?.Trace(
                "ui",
                "select",
                "pick.end",
                ("valid", false),
                ("session", _session?.State));
            return;
        }

        _context = picked;
        var from = _session?.State;
        if (_session is { State: SessionState.Started })
            _session.TransitionTo(SessionState.Discovering);
        if (_session is not null && from is { } previous)
            _record?.TraceState(previous, _session, "pick");

        _record?.Trace(
            "ui",
            "select",
            "pick.end",
            ("valid", true),
            ("origin", _context.Origin),
            ("count", _context.ElementRefs.Count),
            ("session", _session?.State));

        OnPropertyChanged(nameof(Selection));
        OnPropertyChanged(nameof(HasNoElementsInScope));
        OnPropertyChanged(nameof(IsSelectElementsEnabled));
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
