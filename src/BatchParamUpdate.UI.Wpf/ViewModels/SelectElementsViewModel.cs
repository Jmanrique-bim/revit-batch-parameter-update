using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using BatchParamUpdate.Application.Workflow;
using BatchParamUpdate.Domain.Model;
using BatchParamUpdate.Domain.Ports;

namespace BatchParamUpdate.UI.Wpf.ViewModels;

public sealed class SelectElementsViewModel : INotifyPropertyChanged
{
    private readonly IElementSelectionPort _selection;
    private readonly BatchUpdateCoordinator _coordinator;
    private readonly bool _manualPickAllowed;
    private readonly Action? _hideHost;
    private readonly Action? _showHost;

    public SelectElementsViewModel(
        IElementSelectionPort selection,
        BatchUpdateCoordinator coordinator,
        bool manualPickAllowed,
        Action? hideHost = null,
        Action? showHost = null)
    {
        _selection = selection;
        _coordinator = coordinator;
        _manualPickAllowed = manualPickAllowed;
        _hideHost = hideHost;
        _showHost = showHost;
        SelectElementsCommand = new RelayCommand(PickManually, () => IsSelectElementsEnabled);
    }

    public ICommand SelectElementsCommand { get; }

    public bool IsSelectElementsEnabled => _manualPickAllowed;

    public bool HasNoElementsInScope => !_coordinator.State.HasScope;

    public event PropertyChangedEventHandler? PropertyChanged;

    public void NotifyScopeChanged() => OnPropertyChanged(nameof(HasNoElementsInScope));

    private void PickManually()
    {
        _hideHost?.Invoke();
        SelectionContext? picked;
        try
        {
            picked = _selection.PromptManualSelection();
        }
        finally
        {
            _showHost?.Invoke();
        }

        if (picked is { IsValid: true })
            _coordinator.AdoptManualSelection(picked);
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
