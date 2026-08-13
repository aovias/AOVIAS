using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ComplianceServiceManager.Models.SharedRuntime;

/// <summary>UI-facing Shared Runtime snapshot. D5 resource text is advisory only.</summary>
public sealed partial class SharedRuntimeItem : ObservableObject
{
    [ObservableProperty]
    private string profileName = "—";

    [ObservableProperty]
    private string profileId = string.Empty;

    [ObservableProperty]
    private SharedRuntimeStatus status = SharedRuntimeStatus.Stopped;

    [ObservableProperty]
    private string statusDisplay = "Stopped";

    [ObservableProperty]
    private string scopeDisplay = "Shared";

    [ObservableProperty]
    private int serviceCount;

    [ObservableProperty]
    private int toStartCount;

    [ObservableProperty]
    private int reuseCount;

    [ObservableProperty]
    private int blockedCount;

    [ObservableProperty]
    private int ownedCount;

    [ObservableProperty]
    private string healthyDisplay = "—";

    [ObservableProperty]
    private string portsDisplay = "—";

    [ObservableProperty]
    private string resourceDisplay = "—";

    [ObservableProperty]
    private string startedAtDisplay = "—";

    [ObservableProperty]
    private string lastMessage = string.Empty;

    [ObservableProperty]
    private bool canStart;

    [ObservableProperty]
    private bool isStarting;

    public ObservableCollection<SharedRuntimePlanRow> PlanRows { get; } = [];

    public ObservableCollection<string> ValidationLines { get; } = [];

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
            SharedRuntimeStatus.Blocked => "Blocked",
            _ => "Stopped"
        };
        if (message is not null)
        {
            LastMessage = message;
        }
    }
}

public sealed partial class SharedRuntimePlanRow : ObservableObject
{
    [ObservableProperty]
    private int order;

    [ObservableProperty]
    private string serviceName = string.Empty;

    [ObservableProperty]
    private string instanceDisplay = "Shared";

    [ObservableProperty]
    private string portDisplay = "—";

    [ObservableProperty]
    private string actionDisplay = "START";

    [ObservableProperty]
    private string ownerDisplay = "—";

    [ObservableProperty]
    private string reason = string.Empty;
}
