namespace ComplianceServiceManager.Models.Workspaces;

/// <summary>
/// Studio metadata for who owns a configured development workspace.
/// Not inferred from branch names; set by workspace registration.
/// </summary>
public enum WorkspaceAgentOwner
{
    Integration = 0,
    Cursor = 1,
    Claude = 2,
    Unknown = 3
}
