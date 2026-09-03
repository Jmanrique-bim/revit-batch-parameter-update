using System.Windows;
using Velopack;

namespace BatchParamUpdate.Installer;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        // MUST be the very first call — vpk pack verifies this hook exists
        VelopackApp.Build().Run();

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}
