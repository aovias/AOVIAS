using CommunityToolkit.Mvvm.ComponentModel;
using ComplianceServiceManager.Models.ServiceInstances;

namespace ComplianceServiceManager.Models.PortRegistry;

/// <summary>UI-facing reconciled port allocation.</summary>
public sealed partial class PortAllocationItem : ObservableObject
{
    [ObservableProperty]
    private int port;

    [ObservableProperty]
    private string instanceId = string.Empty;

    [ObservableProperty]
    private string serviceId = string.Empty;

    [ObservableProperty]
    private string serviceName = string.Empty;

    [ObservableProperty]
    private string instanceDisplay = "—";

    [ObservableProperty]
    private string? workspaceId;

    [ObservableProperty]
    private string workspaceDisplay = "—";

    [ObservableProperty]
    private string scopeDisplay = "Shared";

    [ObservableProperty]
    private ServiceExecutionScope executionScope = ServiceExecutionScope.Shared;

    [ObservableProperty]
    private PortEffectiveStatus effectiveStatus = PortEffectiveStatus.Unassigned;

    [ObservableProperty]
    private string statusDisplay = "Unassigned";

    [ObservableProperty]
    private string occupancyDisplay = "—";

    [ObservableProperty]
    private int? occupantProcessId;

    [ObservableProperty]
    private string occupantDisplay = "—";

    [ObservableProperty]
    private string? attentionReason;

    [ObservableProperty]
    private bool isAssigned;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public void ApplyEffectiveStatus(PortEffectiveStatus status, string? reason = null)
    {
        EffectiveStatus = status;
        StatusDisplay = status switch
        {
            PortEffectiveStatus.AssignedAvailable => "Assigned / Available",
            PortEffectiveStatus.AssignedOccupied => "Assigned / Occupied",
            PortEffectiveStatus.AssignedConflict => "Assigned / Conflict",
            PortEffectiveStatus.AssignedUnavailable => "Assigned / Unavailable",
            PortEffectiveStatus.OccupiedUnassigned => "Unassigned / Occupied",
            _ => "Unassigned / Available"
        };
        AttentionReason = reason;
    }
}
