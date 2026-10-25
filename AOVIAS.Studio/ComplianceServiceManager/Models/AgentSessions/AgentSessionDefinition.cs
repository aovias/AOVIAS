namespace ComplianceServiceManager.Models.AgentSessions;

/// <summary>Persisted development-agent session metadata. Git/process state is never authoritative here.</summary>
public sealed class AgentSessionDefinition
{
    public string Id { get; set; } = string.Empty;

    public DevelopmentAgentType AgentType { get; set; } = DevelopmentAgentType.Cursor;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string? TaskId { get; set; }

    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Expected branch for this session. Actual branch is always refreshed from Git.</summary>
    public string ExpectedBranch { get; set; } = string.Empty;

    public string? RuntimeProfileId { get; set; }

    public string? PullRequestId { get; set; }

    public AgentSessionStatus Status { get; set; } = AgentSessionStatus.Planned;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public DateTimeOffset? LastObservedAt { get; set; }
}

public sealed class AgentSessionRegistryDocument
{
    public List<AgentSessionDefinition> Sessions { get; set; } = [];
}
