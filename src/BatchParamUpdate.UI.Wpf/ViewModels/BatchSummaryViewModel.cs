using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using BatchParamUpdate.Domain.ErrorCatalog;
using BatchParamUpdate.Domain.Model;

namespace BatchParamUpdate.UI.Wpf.ViewModels;

public sealed class BatchSummaryViewModel : INotifyPropertyChanged
{
    private readonly string _logPath;
    private readonly string _trackerPath;

    public BatchSummaryViewModel(string logPath = "", string trackerPath = "")
    {
        _logPath = logPath;
        _trackerPath = trackerPath;
    }

    public string? Headline { get; private set; }

    public IReadOnlyList<ElementSkip> Skips { get; private set; } = [];

    public bool HasSkips => Skips.Count > 0;

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Show(BatchExecutionResult? result, ErrorCode? error, ReplacementOperation? operation = null)
    {
        if (error is { } code)
        {
            Headline = ErrorWarningCatalog.Message(code);
            Skips = [];
        }
        else if (result?.InstanceOutcome is { } instance)
        {
            Headline = $"Updated {instance.UpdatedCount} element(s). Skipped {instance.Skips.Count}.";
            Skips = instance.Skips;
        }
        else if (result?.TypeOutcome is { } type)
        {
            Headline =
                $"Updated {type.TotalElementsUpdated} element(s) across {type.AffectedTypes.Count} type(s).";
            Skips = [];
        }
        else
        {
            Headline = null;
            Skips = [];
        }

        OnPropertyChanged(nameof(Headline));
        OnPropertyChanged(nameof(Skips));
        OnPropertyChanged(nameof(HasSkips));
        OpenReport(BuildReport(result, error, operation));
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
