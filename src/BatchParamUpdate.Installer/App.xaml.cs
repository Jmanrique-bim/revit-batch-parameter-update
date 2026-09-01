using System.Windows;
using BatchParamUpdate.Installer.ViewModels;
using BatchParamUpdate.Installer.Views;

namespace BatchParamUpdate.Installer;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var window = new InstallerWindow
        {
            DataContext = new InstallerViewModel(new RevitInstallerAdapter())
        };
        window.Show();
    }
}
