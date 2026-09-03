using System.ComponentModel;
using System.Runtime.CompilerServices;
using BatchParamUpdate.Application.Workflow;
using BatchParamUpdate.Domain.Model;

namespace BatchParamUpdate.UI.Wpf.ViewModels;

public sealed class ParameterDiscoveryViewModel : INotifyPropertyChanged
{
    private readonly BatchUpdateCoordinator _coordinator;
    private ParameterCandidate? _selectedInstance;

    public ParameterDiscoveryViewModel(BatchUpdateCoordinator coordinator, SharedSearchViewModel search)
    {
        _coordinator = coordinator;
        Search = search;
        Search.TextChanged += (_, _) => RefreshFilters();
        RefreshFilters();
    }

    public SharedSearchViewModel Search { get; }

    public IReadOnlyList<ParameterCandidate> FilteredInstanceCandidates { get; private set; } = [];

    public bool HasNoInstanceResults => FilteredInstanceCandidates.Count == 0;

    public ParameterCandidate? SelectedInstance
    {
        get => _selectedInstance;
        set
        {
            if (Equals(_selectedInstance, value))
                return;
            _selectedInstance = value;
            OnPropertyChanged();
            if (value is not null)
                _coordinator.ChooseParameter(value);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void RefreshFilters()
    {
        FilteredInstanceCandidates = Search.Query.Matches;
        OnPropertyChanged(nameof(FilteredInstanceCandidates));
        OnPropertyChanged(nameof(HasNoInstanceResults));
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
