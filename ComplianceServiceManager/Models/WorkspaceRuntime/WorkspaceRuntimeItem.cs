using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ComplianceServiceManager.Models.SharedRuntime;
using ComplianceServiceManager.Models.WorkspaceRuntime;

namespace ComplianceServiceManager.Models.WorkspaceRuntime;

public sealed partial class WorkspaceRuntimeItem : ObservableObject
{
    [ObservableProperty]
    private string workspaceId = string.Empty;

    [ObservableProperty]
    private string workspaceDisplay = "—";

    [ObservableProperty]
    private string path = string.Empty;

    [ObservableProperty]
    private string? branch;

    [ObservableProperty]
    private string agentDisplay = "—";

    [ObservableProperty]
    private string profileName = "—";

    [ObservableProperty]
    private SharedRuntimeStatus status = SharedRuntimeStatus.Stopped;

    [ObservableProperty]
    private string statusDisplay = "Stopped";

    [ObservableProperty]
    private int isolatedCount;

    [ObservableProperty]
    private int reuseSharedCount;

    [ObservableProperty]
    private int ownedCount;

    [ObservableProperty]
    private int issueCount;

    [ObservableProperty]
    private bool canStart;

    [ObservableProperty]
    private bool isStarting;

    [ObservableProperty]
    private bool canExecuteStart;

    [ObservableProperty]
    private bool canExecuteStop;

    [ObservableProperty]
    private bool canExecuteCancel;

    [ObservableProperty]
    private string lastMessage = string.Empty;

    [ObservableProperty]
    private string requestedServicesText = string.Empty;

    [ObservableProperty]
    private string? sourceChangedDisplay;

    [ObservableProperty]
    private string runtimeCaption = string.Empty;

    public ObservableCollection<WorkspaceIsolationChoice> IsolationChoices { get; } = [];

    public ObservableCollection<WorkspaceRuntimePlanRow> PlanRows { get; } = [];

    public ObservableCollection<string> ValidationLines { get; } = [];

    public HashSet<string> RequestedServiceNames { get; } = new(StringComparer.OrdinalIgnoreCase);

    public void ApplyStatus(SharedRuntimeStatus next, string? message = null)
    {
        Status = next;
        StatusDisplay = next switch
        {
            SharedRuntimeStatus.Planned => "Planned",
            SharedRuntimeStatus.Validating => "Validating",
            SharedRuntimeStatus.Starting => "Starting",
            SharedRuntimeStatus.Running => "Running",
            SharedRuntimeStatus.Degraded => "Degraded",
            SharedRuntimeStatus.Stopping => "Stopping",
            SharedRuntimeStatus.Failed => "Failed",
            SharedRuntimeStatus.Blocked => IssueCount > 0 ? $"BLOCKED · {IssueCount} issue(s)" : "Blocked",
            _ => "Stopped"
        };
        if (message is not null)
        {
            LastMessage = message;
        }
    }
}

public sealed partial class WorkspaceIsolationChoice : ObservableObject
{
    [ObservableProperty]
    private string serviceName = string.Empty;

    [ObservableProperty]
    private bool isSelected;
}

public sealed partial class WorkspaceRuntimePlanRow : ObservableObject
{
    [ObservableProperty]
    private string serviceName = string.Empty;

    [ObservableProperty]
    private string actionDisplay = string.Empty;

    [ObservableProperty]
    private string scopeDisplay = string.Empty;

    [ObservableProperty]
    private string portDisplay = "—";

    [ObservableProperty]
    private string ownerDisplay = "—";

    [ObservableProperty]
    private string sourcePath = "—";

    [ObservableProperty]
    private string reason = string.Empty;
}
