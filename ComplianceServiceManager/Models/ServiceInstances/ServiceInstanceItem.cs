using CommunityToolkit.Mvvm.ComponentModel;

namespace ComplianceServiceManager.Models.ServiceInstances;

/// <summary>
/// UI-facing reconciled service instance.
/// Definition identity from registry; live branch/path from D1; process/port/state from D2/S3 when applicable.
/// </summary>
public sealed partial class ServiceInstanceItem : ObservableObject
{
    [ObservableProperty]
    private string instanceId = string.Empty;

    [ObservableProperty]
    private string serviceId = string.Empty;

    [ObservableProperty]
    private string serviceName = string.Empty;

    [ObservableProperty]
    private ServiceExecutionScope executionScope = ServiceExecutionScope.Shared;

    [ObservableProperty]
    private ServiceInstanceOwnership ownership = ServiceInstanceOwnership.Shared;

    [ObservableProperty]
    private string? workspaceId;

    [ObservableProperty]
    private string workspaceDisplay = "None";

    [ObservableProperty]
    private string? branch;

    [ObservableProperty]
    private string? sourcePath;

    [ObservableProperty]
    private string? agentSessionId;

    [ObservableProperty]
    private string agentSessionDisplay = "None";

    [ObservableProperty]
    private string? runtimeProfileId;

    [ObservableProperty]
    private string runtimeProfileDisplay = "—";

    [ObservableProperty]
    private int? port;

    [ObservableProperty]
    private string portDisplay = "Unassigned";

    [ObservableProperty]
    private int? processId;

    [ObservableProperty]
    private string processIdDisplay = "—";

    [ObservableProperty]
    private ServiceInstanceState state = ServiceInstanceState.Unknown;

    [ObservableProperty]
    private string stateDisplay = "Unknown";

    [ObservableProperty]
    private string scopeDisplay = "Shared";

    [ObservableProperty]
    private string ownershipDisplay = "Shared";

    [ObservableProperty]
    private string? statusReason;

    /// <summary>True when this row is a placeholder (no registry entry) for a known workspace.</summary>
    [ObservableProperty]
    private bool isConfigured = true;

    [ObservableProperty]
    private string configurationDisplay = "Configured";

    public IList<string> AttentionReasons { get; } = new List<string>();

    public static ServiceInstanceItem FromDefinition(ServiceInstanceDefinition definition)
        => new()
        {
            InstanceId = definition.InstanceId,
            ServiceId = definition.ServiceId,
            ServiceName = definition.ServiceName,
            ExecutionScope = definition.ExecutionScope,
            Ownership = definition.Ownership,
            WorkspaceId = definition.WorkspaceId,
            AgentSessionId = definition.AgentSessionId,
            RuntimeProfileId = definition.RuntimeProfileId,
            ScopeDisplay = FormatScopeDisplay(definition.ExecutionScope, definition.WorkspaceId),
            OwnershipDisplay = FormatOwnershipDisplay(definition.Ownership, definition.WorkspaceId),
            IsConfigured = true,
            ConfigurationDisplay = "Configured"
        };

    public static ServiceInstanceItem CreateNotConfiguredPlaceholder(
        string serviceId,
        string serviceName,
        string workspaceId,
        string workspaceDisplay)
        => new()
        {
            InstanceId = string.Empty,
            ServiceId = serviceId,
            ServiceName = serviceName,
            ExecutionScope = ServiceExecutionScope.Workspace,
            Ownership = ServiceInstanceOwnership.Workspace,
            WorkspaceId = workspaceId,
            WorkspaceDisplay = workspaceDisplay,
            ScopeDisplay = FormatWorkspaceLabel(workspaceId),
            OwnershipDisplay = FormatOwnershipDisplay(ServiceInstanceOwnership.Workspace, workspaceId),
            State = ServiceInstanceState.Defined,
            StateDisplay = "Not Configured",
            PortDisplay = "Unassigned",
            ProcessIdDisplay = "—",
            IsConfigured = false,
            ConfigurationDisplay = "Not Configured"
        };

    public static string FormatWorkspaceLabel(string? workspaceId)
    {
        if (string.Equals(workspaceId, "cursor", StringComparison.OrdinalIgnoreCase))
        {
            return "Cursor";
        }

        if (string.Equals(workspaceId, "claude", StringComparison.OrdinalIgnoreCase))
        {
            return "Claude";
        }

        return string.IsNullOrWhiteSpace(workspaceId) ? "Workspace" : workspaceId.Trim();
    }

    public static string FormatScopeDisplay(ServiceExecutionScope scope, string? workspaceId)
        => scope == ServiceExecutionScope.Shared ? "Shared" : FormatWorkspaceLabel(workspaceId);

    public static string FormatOwnershipDisplay(
        ServiceInstanceOwnership ownership,
        string? workspaceId,
        bool owned = true)
    {
        if (ownership == ServiceInstanceOwnership.Shared)
        {
            return "Shared Runtime";
        }

        return owned ? $"Workspace: {FormatWorkspaceLabel(workspaceId)}" : "External";
    }

    public void ApplyPort(int? value)
    {
        Port = value;
        PortDisplay = value is null ? "Unassigned" : value.Value.ToString();
    }

    public void ApplyProcessId(int? value)
    {
        ProcessId = value;
        ProcessIdDisplay = value is null ? "—" : value.Value.ToString();
    }

    public void ApplyState(ServiceInstanceState next, string? reason = null)
    {
        State = next;
        StateDisplay = next switch
        {
            ServiceInstanceState.Defined => "Defined",
            ServiceInstanceState.Starting => "Starting",
            ServiceInstanceState.Running => "Running",
            ServiceInstanceState.Stopping => "Stopping",
            ServiceInstanceState.Stopped => "Stopped",
            ServiceInstanceState.Failed => "Failed",
            ServiceInstanceState.Unavailable => "Unavailable",
            _ => "Unknown"
        };
        StatusReason = reason;
    }
}
