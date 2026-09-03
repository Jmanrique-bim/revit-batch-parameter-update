using System.ComponentModel;
using System.Runtime.CompilerServices;
using BatchParamUpdate.Application.Workflow;
using BatchParamUpdate.Domain.ErrorCatalog;
using BatchParamUpdate.Domain.Model;

namespace BatchParamUpdate.UI.Wpf.ViewModels;

/// <summary>
/// Owns the child view-models and is the single subscriber to the coordinator. Child view-models
/// never reference each other — they read <see cref="BatchUpdateCoordinator.State"/>.
/// </summary>
public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly BatchUpdateCoordinator _coordinator;
    private ParameterCandidateSet? _lastCandidates;

    public MainViewModel(
        BatchUpdateCoordinator coordinator,
        SelectElementsViewModel select,
        SharedSearchViewModel search,
        ParameterDiscoveryViewModel discovery,
        ReplacementValueViewModel replacement,
        BatchExecutionViewModel execution,
        BatchSummaryViewModel summary)
    {
        _coordinator = coordinator;
        Select = select;
        Search = search;
        Discovery = discovery;
        Replacement = replacement;
        Execution = execution;
        Summary = summary;

        _lastCandidates = coordinator.Candidates;
        Search.ReplaceSet(coordinator.Candidates);
        Search.TextChanged += (_, _) =>
            _coordinator.RecordSearch(Search.Text, [.. Search.Query.Matches.Select(c => c.Name)]);

        _coordinator.Changed += OnCoordinatorChanged;
        Execution.PropertyChanged += OnExecutionChanged;
        OnCoordinatorChanged();
    }

    public SelectElementsViewModel Select { get; }

    public SharedSearchViewModel Search { get; }

    public ParameterDiscoveryViewModel Discovery { get; }

    public ReplacementValueViewModel Replacement { get; }

    public BatchExecutionViewModel Execution { get; }

    public BatchSummaryViewModel Summary { get; }

    public string? ErrorMessage => _coordinator.LastError is { } code
        ? ErrorWarningCatalog.Message(code)
        : null;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnCoordinatorChanged()
    {
        if (!ReferenceEquals(_lastCandidates, _coordinator.Candidates))
        {
            _lastCandidates = _coordinator.Candidates;
            Search.ReplaceSet(_coordinator.Candidates);
            Discovery.ClearSelection();
        }

        OnPropertyChanged(nameof(ErrorMessage));
        Select.NotifyScopeChanged();
        Replacement.NotifyCanRun();
    }

    private void OnExecutionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null or nameof(BatchExecutionViewModel.IsExecuting))
            Select.SetBusy(Execution.IsExecuting);
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
