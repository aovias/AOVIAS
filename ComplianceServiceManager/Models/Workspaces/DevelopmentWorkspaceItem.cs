using CommunityToolkit.Mvvm.ComponentModel;
using ComplianceServiceManager.Models;

namespace ComplianceServiceManager.Models.Workspaces;

/// <summary>
/// UI-facing development workspace: configured Studio metadata + last discovered Git state.
/// </summary>
public sealed partial class DevelopmentWorkspaceItem : ObservableObject
{
    [ObservableProperty]
    private string id = string.Empty;

    [ObservableProperty]
    private string displayName = string.Empty;

    [ObservableProperty]
    private string path = string.Empty;

    [ObservableProperty]
    private string repositoryId = string.Empty;

    [ObservableProperty]
    private string repositoryName = string.Empty;

    [ObservableProperty]
    private WorkspaceAgentOwner agentOwner = WorkspaceAgentOwner.Unknown;

    [ObservableProperty]
    private WorkspaceRole role = WorkspaceRole.Other;

    [ObservableProperty]
    private bool isMain;

    [ObservableProperty]
    private string? branch;

    [ObservableProperty]
    private string? head;

    [ObservableProperty]
    private WorkspaceAvailability availability = WorkspaceAvailability.Unavailable;

    [ObservableProperty]
    private string statusDisplay = "UNAVAILABLE";

    [ObservableProperty]
    private string? statusReason;

    [ObservableProperty]
    private int? ahead;

    [ObservableProperty]
    private int? behind;

    [ObservableProperty]
    private DateTimeOffset? lastRefresh;

    [ObservableProperty]
    private string agentOwnerDisplay = "Unknown";

    [ObservableProperty]
    private string roleDisplay = "Other";

    [ObservableProperty]
    private string headDisplay = "—";

    [ObservableProperty]
    private string aheadBehindDisplay = "—";

    public void ApplyGitState(GitWorkspaceState state)
    {
        Branch = state.Branch;
        Head = state.Head;
        Ahead = state.Ahead;
        Behind = state.Behind;
        LastRefresh = state.ObservedAt;
        HeadDisplay = string.IsNullOrWhiteSpace(state.Head)
            ? "—"
            : state.Head.Length <= 12
                ? state.Head
                : state.Head[..12];

        if (!state.IsAccessible)
        {
            Availability = WorkspaceAvailability.Unavailable;
            StatusDisplay = "UNAVAILABLE";
            StatusReason = string.IsNullOrWhiteSpace(state.Error)
                ? "Git repository cannot be accessed."
                : state.Error;
            AheadBehindDisplay = "—";
            return;
        }

        if (state.IsDetached)
        {
            Availability = WorkspaceAvailability.Detached;
            StatusDisplay = "DETACHED";
            StatusReason = "HEAD is detached.";
        }
        else if (state.IsClean)
        {
            Availability = WorkspaceAvailability.Clean;
            StatusDisplay = "CLEAN";
            StatusReason = null;
        }
        else
        {
            Availability = WorkspaceAvailability.Dirty;
            StatusDisplay = "DIRTY";
            StatusReason = "Working tree has uncommitted changes.";
        }

        AheadBehindDisplay = state.Ahead is null && state.Behind is null
            ? "—"
            : $"↑{state.Ahead ?? 0} ↓{state.Behind ?? 0}";
    }

    public void ApplyConfigurationDisplays()
    {
        DisplayName = OperatorLabels.Workspace(Id, AgentOwner);
        AgentOwnerDisplay = OperatorLabels.Agent(AgentOwner);

        RoleDisplay = Role switch
        {
            WorkspaceRole.Integration => "Integration",
            WorkspaceRole.Agent => "Agent",
            _ => "Other"
        };
    }
}
