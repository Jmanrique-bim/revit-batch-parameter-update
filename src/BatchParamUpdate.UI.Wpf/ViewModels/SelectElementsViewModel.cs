using BatchParamUpdate.Domain.Model;

namespace BatchParamUpdate.UI.Wpf.ViewModels;

public sealed class SelectElementsViewModel
{
    public SelectElementsViewModel(SelectionContext selection)
        => IsSelectElementsEnabled = selection.Origin == SelectionOrigin.ManualPick;

    public bool IsSelectElementsEnabled { get; }
}
