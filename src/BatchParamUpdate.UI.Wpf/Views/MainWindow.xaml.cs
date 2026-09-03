using BatchParamUpdate.UI.Wpf.ViewModels;

namespace BatchParamUpdate.UI.Wpf.Views;

public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public void Bind(MainViewModel vm)
    {
        DataContext = vm;
        SelectElementsButton.DataContext = vm.Select;
        EmptyScopeBanner.DataContext = vm.Select;
        SharedSearchBox.DataContext = vm.Search;
        ParameterPanel.DataContext = vm.Discovery;
        AdvanceErrorBanner.DataContext = vm;
        ReplacementPanel.DataContext = vm.Replacement;
        ExecutionProgress.DataContext = vm.Execution;
        SummaryPanel.DataContext = vm.Summary;
    }
}
