using System.ComponentModel;
using System.Runtime.CompilerServices;
using BatchParamUpdate.Domain.Model;

namespace BatchParamUpdate.Application.Workflow;

/// <summary>
/// The single shared model the child view-models read from and the coordinator writes to.
/// Replaces cross-view-model <c>Func&lt;&gt;</c> callbacks — no screen references another screen.
/// </summary>
public sealed class WorkflowState : INotifyPropertyChanged
{
    private SelectionContext _scope = new([], SelectionOrigin.PreExisting);
    private ParameterCandidate? _target;
    private string _newValue = "";

    public SelectionContext Scope
    {
        get => _scope;
        private set { _scope = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasScope)); }
    }

    public ParameterCandidate? Target
    {
        get => _target;
        private set { _target = value; OnPropertyChanged(); }
    }

    public string NewValue
    {
        get => _newValue;
        private set { _newValue = value; OnPropertyChanged(); }
    }

    public bool HasScope => _scope.IsValid;

    public event PropertyChangedEventHandler? PropertyChanged;

    internal void SetScope(SelectionContext scope) => Scope = scope;

    internal void SetTarget(ParameterCandidate? target) => Target = target;

    internal void SetNewValue(string value) => NewValue = value ?? "";

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
