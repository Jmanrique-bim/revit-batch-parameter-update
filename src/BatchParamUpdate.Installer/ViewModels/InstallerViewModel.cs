using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using BatchParamUpdate.Domain.Model;
using BatchParamUpdate.Domain.Ports;

namespace BatchParamUpdate.Installer.ViewModels;

public sealed class InstallerViewModel : INotifyPropertyChanged
{
    private readonly IInstallerPort _port;
    private string _status = "Select a detected Revit year.";

    public InstallerViewModel(IInstallerPort port)
    {
        _port = port;
        var package = new InstallerPackage(port.DetectInstalledRevitYears());
        Years = package.DetectedRevitYears
            .Select(year => new DetectedRevitYearViewModel(year, this, port))
            .ToArray();
        if (Years.Count == 0)
            Status = "No supported Revit version (2025 or 2026) was detected.";
    }

    public IReadOnlyList<DetectedRevitYearViewModel> Years { get; }

    public string Status
    {
        get => _status;
        private set
        {
            if (_status == value)
                return;
            _status = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    internal void Run(int year, InstallerAction action)
    {
        try
        {
            switch (action)
            {
                case InstallerAction.Install:
                    _port.Install(year);
                    break;
                case InstallerAction.Update:
                    _port.Update(year);
                    break;
                case InstallerAction.Uninstall:
                    _port.Uninstall(year);
                    break;
            }

            foreach (var row in Years)
                row.Refresh();
            Status = $"{action} completed for Revit {year}.";
        }
        catch (Exception ex)
        {
            Status = $"{action} failed for Revit {year}: {ex.Message}";
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed class DetectedRevitYearViewModel : INotifyPropertyChanged
{
    private readonly IInstallerPort _port;
    private bool _addinInstalled;

    public DetectedRevitYearViewModel(int year, InstallerViewModel owner, IInstallerPort port)
    {
        Year = year;
        _port = port;
        _addinInstalled = port.IsAddinInstalled(year);
        PrimaryCommand = new ActionCommand(() =>
            owner.Run(year, _addinInstalled ? InstallerAction.Update : InstallerAction.Install));
        UninstallCommand = new ActionCommand(() => owner.Run(year, InstallerAction.Uninstall));
    }

    public int Year { get; }

    public string PrimaryLabel => _addinInstalled ? "Update" : "Install";

    public string YearStatus => _addinInstalled ? "Installed" : "Not installed";

    public ICommand PrimaryCommand { get; }

    public ICommand UninstallCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    internal void Refresh()
    {
        _addinInstalled = _port.IsAddinInstalled(Year);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PrimaryLabel)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(YearStatus)));
    }
}

internal sealed class ActionCommand(Action execute) : ICommand
{
    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => execute();

    public event EventHandler? CanExecuteChanged
    {
        add { }
        remove { }
    }
}
