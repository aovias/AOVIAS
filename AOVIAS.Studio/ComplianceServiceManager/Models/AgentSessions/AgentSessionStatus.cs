namespace ComplianceServiceManager.Models.AgentSessions;

/// <summary>Lifecycle status for a development-agent session (metadata only).</summary>
public enum AgentSessionStatus
{
    Planned = 0,
    Active = 1,
    Paused = 2,
    Completed = 3,
    Blocked = 4,
    Cancelled = 5,
    Stale = 6
}

/// <summary>Derived health after reconciliation with Git/workspaces/profiles/PRs.</summary>
public enum AgentSessionHealth
{
    Unknown = 0,
    Healthy = 1,
    NeedsAttention = 2
}
