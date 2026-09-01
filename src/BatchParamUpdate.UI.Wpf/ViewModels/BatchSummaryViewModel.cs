using System.ComponentModel;
using System.Runtime.CompilerServices;
using BatchParamUpdate.Domain.ErrorCatalog;
using BatchParamUpdate.Domain.Model;

namespace BatchParamUpdate.UI.Wpf.ViewModels;

public sealed class BatchSummaryViewModel : INotifyPropertyChanged
{
    public string? Headline { get; private set; }

    public IReadOnlyList<ElementSkip> Skips { get; private set; } = [];

    public bool HasSkips => Skips.Count > 0;

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Show(BatchExecutionResult? result, ErrorCode? error)
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
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
