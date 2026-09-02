using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BatchParamUpdate.Application.UseCases;
using BatchParamUpdate.Domain.ErrorCatalog;
using BatchParamUpdate.Domain.Model;

namespace BatchParamUpdate.UI.Wpf.ViewModels;

public sealed class BatchSummaryViewModel : INotifyPropertyChanged
{
    // Report Panel · Variant C (paginated table + CSV export): a run can
    // skip hundreds of elements, so the summary never renders the raw
    // Skips collection directly — it always goes through SearchText →
    // PagedSkips, PageSize rows at a time.
    private const int PageSize = 20;

    private readonly string _logPath;
    private readonly string _trackerPath;
    private readonly string _runId;
    private readonly ExportSkipReportUseCase? _exportUseCase;

    private IReadOnlyList<ElementSkip> _allSkips = [];
    private IReadOnlyList<ElementSkip> _filteredSkips = [];
    private string _searchText = "";
    private int _pageIndex;
    private string? _exportStatusMessage;

    public BatchSummaryViewModel(
        string logPath = "",
        string trackerPath = "",
        string runId = "",
        ExportSkipReportUseCase? exportUseCase = null)
    {
        _logPath = logPath;
        _trackerPath = trackerPath;
        _runId = runId;
        _exportUseCase = exportUseCase;
        NextPageCommand = new RelayCommand(() => ChangePage(1), () => CanGoNext);
        PreviousPageCommand = new RelayCommand(() => ChangePage(-1), () => CanGoPrevious);
        ExportCommand = new RelayCommand(Export, () => HasSkips && _exportUseCase is not null);
    }

    public string? Headline { get; private set; }

    /// <summary>All skips from the last run. Prefer <see cref="PagedSkips"/> for display.</summary>
    public IReadOnlyList<ElementSkip> Skips => _allSkips;

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

    public void Show(BatchExecutionResult? result, ErrorCode? error, ReplacementOperation? operation = null)
    {
        ExportStatusMessage = null;

        if (error is { } code)
        {
            Headline = ErrorWarningCatalog.Message(code);
            _allSkips = [];
        }
        else if (result?.InstanceOutcome is { } instance)
        {
            Headline = $"Updated {instance.UpdatedCount} element(s). Skipped {instance.Skips.Count}.";
            _allSkips = instance.Skips;
        }
        else if (result?.TypeOutcome is { } type)
        {
            Headline =
                $"Updated {type.TotalElementsUpdated} element(s) across {type.AffectedTypes.Count} type(s).";
            _allSkips = [];
        }
        else
        {
            Headline = null;
            _allSkips = [];
        }

        _searchText = "";
        OnPropertyChanged(nameof(Headline));
        OnPropertyChanged(nameof(Skips));
        OnPropertyChanged(nameof(HasSkips));
        OnPropertyChanged(nameof(SearchText));
        ApplyFilter(resetPage: true);
        CommandManager.InvalidateRequerySuggested();
        OpenReport(BuildReport(result, error, operation));
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
        CommandManager.InvalidateRequerySuggested();
    }

    private void Export()
    {
        if (_exportUseCase is null)
            return;

        var path = _exportUseCase.Execute(_allSkips, _runId);
        ExportStatusMessage = path is null
            ? null
            : $"Exported {_allSkips.Count} row(s) to {path}";
    }

    private string BuildReport(BatchExecutionResult? result, ErrorCode? error, ReplacementOperation? operation)
    {
        var sb = new StringBuilder();
        if (Headline is not null)
            sb.AppendLine(Headline);
        if (error is { } code)
            sb.AppendLine($"{ErrorWarningCatalog.Code(code)}");
        if (operation is not null)
        {
            sb.AppendLine($"Parameter: {operation.TargetParameter.Name} ({operation.TargetParameter.Binding})");
            sb.AppendLine($"Replacement: {operation.NewValue}");
        }

        if (result?.TypeOutcome is { } type)
        {
            foreach (var affected in type.AffectedTypes)
                sb.AppendLine($"Type: {affected.Name}");
        }

        foreach (var skip in Skips)
            sb.AppendLine($"{skip.Element.DisplayLabel} — {skip.Message}");

        if (_logPath.Length > 0)
            sb.AppendLine($"Log: {_logPath}");
        if (_trackerPath.Length > 0)
            sb.AppendLine($"Tracker: {_trackerPath}");
        return sb.ToString().TrimEnd();
    }

    private static void OpenReport(string report)
    {
        if (report.Length == 0)
            return;

        var folder = Path.Combine(Path.GetTempPath(), "juanManriqueHexagon");
        var open = new Button
        {
            Content = "Open log folder",
            Padding = new Thickness(12, 6, 12, 6),
            Margin = new Thickness(12),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        open.Click += (_, _) =>
        {
            Directory.CreateDirectory(folder);
            Process.Start(new ProcessStartInfo { FileName = folder, UseShellExecute = true });
        };

        var box = new TextBox
        {
            Text = report,
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(12)
        };
        DockPanel.SetDock(open, Dock.Bottom);
        var panel = new DockPanel();
        panel.Children.Add(open);
        panel.Children.Add(box);
        new Window
        {
            Title = "Batch update summary",
            Width = 560,
            Height = 420,
            Content = panel
        }.Show();
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
