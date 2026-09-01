using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;
using BatchParamUpdate.Adapters.Revit.ExternalCommand;

namespace BatchParamUpdate.Adapters.Revit;

public sealed class App : IExternalApplication
{
    public Result OnStartup(UIControlledApplication application)
    {
        var panel = application.CreateRibbonPanel("Batch Parameter Update");
        var data = new PushButtonData(
            "BatchParameterUpdate",
            "Batch Update",
            Assembly.GetExecutingAssembly().Location,
            typeof(BatchParameterUpdateCommand).FullName);

        if (panel.AddItem(data) is PushButton button)
        {
            button.Image = LoadPng("icons8-optimization-64.png", decodePx: 16);
            button.LargeImage = LoadPng("icons8-optimization-100.png", decodePx: 32);
        }

        return Result.Succeeded;
    }

    public Result OnShutdown(UIControlledApplication application) => Result.Succeeded;

    private static BitmapImage LoadPng(string fileName, int decodePx)
    {
        var resource = $"BatchParamUpdate.Adapters.Revit.Resources.{fileName}";
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"Missing embedded resource '{resource}'.");

        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.DecodePixelWidth = decodePx;
        image.EndInit();
        image.Freeze();
        return image;
    }
}
