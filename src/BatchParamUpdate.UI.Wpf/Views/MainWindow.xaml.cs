using BatchParamUpdate.UI.Wpf.ViewModels;

namespace BatchParamUpdate.UI.Wpf.Views;

public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public void Bind(
        SelectElementsViewModel select,
        SharedSearchViewModel search,
        ParameterDiscoveryViewModel discovery,
        ReplacementValueViewModel replacement,
        BatchExecutionViewModel execution,
        BatchSummaryViewModel summary)
    {
        SelectElementsButton.DataContext = select;
        EmptyScopeBanner.DataContext = select;
        SharedSearchBox.DataContext = search;
        InstanceDialog.DataContext = discovery;
        TypeDialog.DataContext = discovery;
        ChooseParameterButton.DataContext = discovery;
        AdvanceErrorBanner.DataContext = discovery;
        ReplacementPanel.DataContext = replacement;
        ExecutionProgress.DataContext = execution;
        SummaryPanel.DataContext = summary;
    }
}
