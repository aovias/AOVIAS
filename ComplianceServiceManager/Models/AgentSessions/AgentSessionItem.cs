using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ComplianceServiceManager.Models.AgentSessions;

/// <summary>UI-facing development-agent session with live reconciliation fields.</summary>
public sealed partial class AgentSessionItem : ObservableObject
{
    [ObservableProperty]
    private string id = string.Empty;

    [ObservableProperty]
    private DevelopmentAgentType agentType = DevelopmentAgentType.Cursor;

    [ObservableProperty]
    private string agentDisplay = "Cursor";

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private string? taskId;

    [ObservableProperty]
    private string workspaceId = string.Empty;

    [ObservableProperty]
    private string workspaceDisplay = string.Empty;

    [ObservableProperty]
    private string workspacePath = string.Empty;

    [ObservableProperty]
    private string expectedBranch = string.Empty;

    [ObservableProperty]
    private string? actualBranch;

    [ObservableProperty]
    private string? runtimeProfileId;

    [ObservableProperty]
    private string runtimeProfileDisplay = "—";

    [ObservableProperty]
    private string? pullRequestId;

    [ObservableProperty]
    private string pullRequestDisplay = "—";

    [ObservableProperty]
    private AgentSessionStatus status = AgentSessionStatus.Planned;

    [ObservableProperty]
    private string statusDisplay = "PLANNED";

    [ObservableProperty]
    private AgentSessionHealth health = AgentSessionHealth.Unknown;

    [ObservableProperty]
    private string healthDisplay = "UNKNOWN";

    [ObservableProperty]
    private string resourcesDisplay = "—";

    [ObservableProperty]
    private string summaryDisplay = string.Empty;

    public ObservableCollection<string> AttentionReasons { get; } = [];

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public DateTimeOffset? LastObservedAt { get; set; }

    public void ApplyStatus(AgentSessionStatus next)
    {
        Status = next;
        StatusDisplay = next switch
        {
            AgentSessionStatus.Planned => "PLANNED",
            AgentSessionStatus.Active => "ACTIVE",
            AgentSessionStatus.Paused => "PAUSED",
            AgentSessionStatus.Completed => "COMPLETED",
            AgentSessionStatus.Blocked => "BLOCKED",
            AgentSessionStatus.Cancelled => "CANCELLED",
            AgentSessionStatus.Stale => "STALE",
            _ => next.ToString().ToUpperInvariant()
        };
    }

    public void ApplyHealth(AgentSessionHealth next, IEnumerable<string> reasons)
    {
        Health = next;
        HealthDisplay = next switch
        {
            AgentSessionHealth.Healthy => "HEALTHY",
            AgentSessionHealth.NeedsAttention => "NEEDS ATTENTION",
            _ => "UNKNOWN"
        };
        AttentionReasons.Clear();
        foreach (var reason in reasons)
        {
            AttentionReasons.Add(reason);
        }
    }

    public AgentSessionDefinition ToDefinition()
        => new()
        {
            Id = Id,
            AgentType = AgentType,
            Title = Title,
            Description = Description,
            TaskId = TaskId,
            WorkspaceId = WorkspaceId,
            ExpectedBranch = ExpectedBranch,
            RuntimeProfileId = RuntimeProfileId,
            PullRequestId = PullRequestId,
            Status = Status,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            StartedAt = StartedAt,
            CompletedAt = CompletedAt,
            LastObservedAt = LastObservedAt
        };

    public static AgentSessionItem FromDefinition(AgentSessionDefinition definition)
    {
        var item = new AgentSessionItem
        {
            Id = definition.Id,
            AgentType = definition.AgentType,
            AgentDisplay = definition.AgentType.ToString(),
            Title = definition.Title,
            Description = definition.Description,
            TaskId = definition.TaskId,
            WorkspaceId = definition.WorkspaceId,
            ExpectedBranch = definition.ExpectedBranch,
            RuntimeProfileId = definition.RuntimeProfileId,
            PullRequestId = definition.PullRequestId,
            CreatedAt = definition.CreatedAt,
            UpdatedAt = definition.UpdatedAt,
            StartedAt = definition.StartedAt,
            CompletedAt = definition.CompletedAt,
            LastObservedAt = definition.LastObservedAt
        };
        item.ApplyStatus(definition.Status);
        return item;
    }
}
