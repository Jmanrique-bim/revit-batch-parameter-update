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
            .Select(year => new DetectedRevitYearViewModel(year, this))
            .ToArray();
        if (Years.Count == 0)
            Status = "No supported Revit version (2025, 2026, or 2027) was detected.";
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

public sealed class DetectedRevitYearViewModel
{
    public DetectedRevitYearViewModel(int year, InstallerViewModel owner)
    {
        Year = year;
        InstallCommand = new ActionCommand(() => owner.Run(year, InstallerAction.Install));
        UpdateCommand = new ActionCommand(() => owner.Run(year, InstallerAction.Update));
        UninstallCommand = new ActionCommand(() => owner.Run(year, InstallerAction.Uninstall));
    }

    public int Year { get; }

    public ICommand InstallCommand { get; }

    public ICommand UpdateCommand { get; }

    public ICommand UninstallCommand { get; }
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
