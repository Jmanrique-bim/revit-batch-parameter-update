using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using BatchParamUpdate.Domain.ErrorCatalog;
using BatchParamUpdate.Domain.Model;
using BatchParamUpdate.Domain.Ports;

namespace BatchParamUpdate.UI.Wpf.ViewModels;

public sealed class BatchSummaryViewModel : INotifyPropertyChanged
{
    // Report Panel · Variant C (paginated table + CSV export): a run can
    // skip hundreds of elements, so the summary never renders the raw
    // skip list directly — it always goes through SearchText → PagedSkips,
    // PageSize rows at a time. The in-window panel is the only summary;
    // there is no second popup window.
    private const int PageSize = 20;

    private readonly IReportExportPort _export;
    private readonly string _runId;

    private IReadOnlyList<ElementSkip> _allSkips = [];
    private IReadOnlyList<ElementSkip> _filteredSkips = [];
    private string _searchText = "";
    private int _pageIndex;
    private string? _exportStatusMessage;

    public BatchSummaryViewModel(IReportExportPort export, string runId)
    {
        _export = export;
        _runId = runId;
        NextPageCommand = new RelayCommand(() => ChangePage(1), () => CanGoNext);
        PreviousPageCommand = new RelayCommand(() => ChangePage(-1), () => CanGoPrevious);
        ExportCommand = new RelayCommand(Export, () => HasSkips);
    }

    public string? Headline { get; private set; }

    public bool HasSkips => _allSkips.Count > 0;

    /// <summary>Filters skips by element label, category, or message. Resets to page 1.</summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            var next = value ?? "";
            if (_searchText == next)
                return;
            _searchText = next;
            OnPropertyChanged();
            ApplyFilter(resetPage: true);
        }
    }

    /// <summary>The current page (<see cref="PageSize"/> rows) of the filtered skip list — what the grid binds to.</summary>
    public IReadOnlyList<ElementSkip> PagedSkips { get; private set; } = [];

    public int FilteredCount => _filteredSkips.Count;

    public int PageNumber => _pageIndex + 1;

    public int TotalPages => Math.Max(1, (int)Math.Ceiling(_filteredSkips.Count / (double)PageSize));

    public string PageSummary => FilteredCount == 0
        ? "0 elements"
        : $"{(_pageIndex * PageSize) + 1}–{Math.Min((_pageIndex + 1) * PageSize, FilteredCount)} of {FilteredCount}";

    public bool CanGoNext => _pageIndex < TotalPages - 1;

    public bool CanGoPrevious => _pageIndex > 0;

    public string? ExportStatusMessage
    {
        get => _exportStatusMessage;
        private set
        {
            if (_exportStatusMessage == value)
                return;
            _exportStatusMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasExportStatusMessage));
        }
    }

    public bool HasExportStatusMessage => !string.IsNullOrEmpty(_exportStatusMessage);

    public ICommand NextPageCommand { get; }

    public ICommand PreviousPageCommand { get; }

    public ICommand ExportCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Show(BatchExecutionResult? result, ErrorCode? error)
    {
        ExportStatusMessage = null;

        if (result is { RolledBack: true })
        {
            // A revert still carries per-element skips — keep them so the grid and CSV export
            // work. Run() always passes LastError (BatchRolledBack after a revert), so this
            // branch has to win over the generic error branch below.
            Headline = ErrorWarningCatalog.Message(ErrorCode.BatchRolledBack);
            _allSkips = result.Skips;
        }
        else if (error is { } code)
        {
            Headline = ErrorWarningCatalog.Message(code);
            _allSkips = [];
        }
        else if (result is not null)
        {
            Headline = $"Updated {result.UpdatedCount} element(s). Skipped {result.Skips.Count}.";
            _allSkips = result.Skips;
        }
        else
        {
            Headline = null;
            _allSkips = [];
        }

        _searchText = "";
        OnPropertyChanged(nameof(Headline));
        OnPropertyChanged(nameof(HasSkips));
        OnPropertyChanged(nameof(SearchText));
        ApplyFilter(resetPage: true);
    }

    private void ApplyFilter(bool resetPage)
    {
        var query = _searchText.Trim();
        if (query.Length == 0)
            _filteredSkips = _allSkips;
        else
            _filteredSkips = [.. _allSkips.Where(s => Matches(s, query))];

        if (resetPage)
            _pageIndex = 0;
        else if (_pageIndex > TotalPages - 1)
            _pageIndex = Math.Max(0, TotalPages - 1);

        RaisePagingChanged();
    }

    private static bool Matches(ElementSkip skip, string query) =>
        skip.Element.DisplayLabel.Contains(query, StringComparison.OrdinalIgnoreCase)
        || skip.Element.CategoryName.Contains(query, StringComparison.OrdinalIgnoreCase)
        || skip.Message.Contains(query, StringComparison.OrdinalIgnoreCase);

    private void ChangePage(int direction)
    {
        var next = _pageIndex + direction;
        if (next < 0 || next > TotalPages - 1)
            return;

        _pageIndex = next;
        RaisePagingChanged();
    }

    private void RaisePagingChanged()
    {
        PagedSkips = [.. _filteredSkips.Skip(_pageIndex * PageSize).Take(PageSize)];
        OnPropertyChanged(nameof(PagedSkips));
        OnPropertyChanged(nameof(FilteredCount));
        OnPropertyChanged(nameof(PageNumber));
        OnPropertyChanged(nameof(TotalPages));
        OnPropertyChanged(nameof(PageSummary));
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(CanGoPrevious));
        RefreshCommands();
    }

    private void RefreshCommands()
    {
        (NextPageCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (PreviousPageCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (ExportCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    private void Export()
    {
        var path = _export.ExportSkips(_allSkips, _runId);
        ExportStatusMessage = $"Exported {_allSkips.Count} row(s) to {path}";
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
