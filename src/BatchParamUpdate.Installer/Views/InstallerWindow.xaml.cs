using System.Windows;
using System.Windows.Input;

namespace BatchParamUpdate.Installer.Views;

public partial class InstallerWindow : Window
{
    public InstallerWindow()
    {
        InitializeComponent();
    }

    private void OnDrag(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}
