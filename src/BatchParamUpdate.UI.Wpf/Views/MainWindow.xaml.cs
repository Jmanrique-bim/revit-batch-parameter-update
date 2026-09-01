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
        ParameterDiscoveryViewModel discovery)
    {
        SelectElementsButton.DataContext = select;
        EmptyScopeBanner.DataContext = select;
        SharedSearchBox.DataContext = search;
        InstanceDialog.DataContext = discovery;
        TypeDialog.DataContext = discovery;
        ChooseParameterButton.DataContext = discovery;
        AdvanceErrorBanner.DataContext = discovery;
    }
}
