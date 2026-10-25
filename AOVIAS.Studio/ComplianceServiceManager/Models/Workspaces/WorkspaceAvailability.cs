namespace ComplianceServiceManager.Models.Workspaces;

/// <summary>Derived availability of a workspace after a Git refresh.</summary>
public enum WorkspaceAvailability
{
    Clean = 0,
    Dirty = 1,
    Unavailable = 2,
    Detached = 3
}
