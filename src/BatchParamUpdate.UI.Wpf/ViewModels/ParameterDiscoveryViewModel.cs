using System.ComponentModel;
using System.Runtime.CompilerServices;
using BatchParamUpdate.Application.UseCases;
using BatchParamUpdate.Domain.ErrorCatalog;
using BatchParamUpdate.Domain.Model;

namespace BatchParamUpdate.UI.Wpf.ViewModels;

public sealed class ParameterDiscoveryViewModel : INotifyPropertyChanged
{
    private readonly DiscoverParametersUseCase _useCase;
    private SelectionContext _scope;
    private readonly Session _session;
    private ParameterCandidate? _selectedInstance;
    private ParameterCandidate? _selectedType;

    public ParameterDiscoveryViewModel(
        DiscoverParametersUseCase useCase,
        SelectionContext scope,
        Session session,
        SharedSearchViewModel search)
    {
        _useCase = useCase;
        _scope = scope;
        _session = session;
        Search = search;
        Search.TextChanged += (_, _) => RefreshFilters();
        RefreshFilters();
    }

    public SharedSearchViewModel Search { get; }

    public IReadOnlyList<ParameterCandidate> FilteredInstanceCandidates { get; private set; } = [];

    public IReadOnlyList<ParameterCandidate> FilteredTypeCandidates { get; private set; } = [];

    public bool HasNoInstanceResults => FilteredInstanceCandidates.Count == 0;

    public bool HasNoTypeResults => FilteredTypeCandidates.Count == 0;

    public ParameterCandidate? SelectedInstance
    {
        get => _selectedInstance;
        set
        {
            if (Equals(_selectedInstance, value))
                return;
            _selectedInstance = value;
            if (value is not null && _selectedType is not null)
            {
                _selectedType = null;
                OnPropertyChanged(nameof(SelectedType));
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowWideBlastWarning));
            if (value is not null)
                Advance();
        }
    }

    public ParameterCandidate? SelectedType
    {
        get => _selectedType;
        set
        {
            if (Equals(_selectedType, value))
                return;
            _selectedType = value;
            if (value is not null && _selectedInstance is not null)
            {
                _selectedInstance = null;
                OnPropertyChanged(nameof(SelectedInstance));
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowWideBlastWarning));
            if (value is not null)
                Advance();
        }
    }

    public bool ShowWideBlastWarning => SelectedType is not null;

    public string? AdvanceErrorMessage { get; private set; }

    public ReplacementOperation? Operation { get; private set; }

    public string CurrentValueSummary
    {
        get
        {
            if (Operation is null)
                return "";

            var name = Operation.TargetParameter.Name;
            var values = Operation.TargetParameter.ObservedValues
                .Select(v => string.IsNullOrEmpty(v) ? "(empty)" : v)
                .Distinct(StringComparer.Ordinal)
                .ToList();
            if (values.Count == 0)
                return $"No current value for {name}.";
            if (values.Count == 1)
                return $"Current value of {name}: {values[0]}";
            return $"Current values of {name}: {string.Join(", ", values)}";
        }
    }

    public bool HasCurrentValueSummary => Operation is not null;

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Retarget(SelectionContext scope)
    {
        _scope = scope;
        RefreshFilters();
    }

    private void RefreshFilters()
    {
        FilteredInstanceCandidates = Search.Query.MatchesInstanceSet;
        FilteredTypeCandidates = Search.Query.MatchesTypeSet;
        OnPropertyChanged(nameof(FilteredInstanceCandidates));
        OnPropertyChanged(nameof(FilteredTypeCandidates));
        OnPropertyChanged(nameof(HasNoInstanceResults));
        OnPropertyChanged(nameof(HasNoTypeResults));
    }

    private void Advance()
    {
        var chosen = SelectedInstance ?? SelectedType;
        Operation = _useCase.Choose(chosen, _scope, _session);
        AdvanceErrorMessage = _useCase.Error is { } error
            ? ErrorWarningCatalog.Message(error)
            : null;
        OnPropertyChanged(nameof(AdvanceErrorMessage));
        OnPropertyChanged(nameof(Operation));
        OnPropertyChanged(nameof(CurrentValueSummary));
        OnPropertyChanged(nameof(HasCurrentValueSummary));
        OnPropertyChanged(nameof(ShowWideBlastWarning));
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
