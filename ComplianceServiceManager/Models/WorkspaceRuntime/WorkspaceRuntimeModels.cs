using ComplianceServiceManager.Models.SharedRuntime;

namespace ComplianceServiceManager.Models.WorkspaceRuntime;

public enum WorkspaceRuntimeAction
{
    StartWorkspace = 0,
    ReuseShared = 1,
    ReuseWorkspace = 2,
    Blocked = 3
}

public sealed class WorkspaceRuntimeValidationIssue
{
    public required string Code { get; init; }

    public required string Message { get; init; }

    public string? ServiceName { get; init; }

    public string? InstanceId { get; init; }

    public int? Port { get; init; }

    public string? Phase { get; init; }

    public bool Blocking { get; init; } = true;
}

public sealed class WorkspaceRuntimePlanEntry
{
    public string ServiceName { get; init; } = string.Empty;

    public string? InstanceId { get; init; }

    public string ScopeDisplay { get; init; } = "Workspace";

    public int? Port { get; init; }

    public WorkspaceRuntimeAction Action { get; init; }

    public string ActionDisplay { get; init; } = "BLOCK";

    public string? Reason { get; init; }

    public bool OwnedAfterStart { get; init; }

    public string OwnerDisplay { get; init; } = "—";

    public string? SourcePath { get; init; }
}

public sealed class WorkspaceRuntimePlan
{
    public string WorkspaceId { get; init; } = string.Empty;

    public string WorkspaceDisplay { get; init; } = string.Empty;

    public string? Branch { get; init; }

    public string ProfileId { get; init; } = string.Empty;

    public string ProfileName { get; init; } = string.Empty;

    public string SourceRoot { get; init; } = string.Empty;

    public string? AgentSessionId { get; init; }

    public bool CanStart { get; init; }

    public IReadOnlyList<string> StartupOrder { get; init; } = [];

    public IReadOnlyList<WorkspaceRuntimePlanEntry> Entries { get; init; } = [];

    public IReadOnlyList<WorkspaceRuntimeValidationIssue> Issues { get; init; } = [];

    public IReadOnlyList<string> Warnings { get; init; } = [];

    public int IssueCount => Issues.Count(i => i.Blocking);

    public int IsolatedCount => Entries.Count(e => e.Action is WorkspaceRuntimeAction.StartWorkspace or WorkspaceRuntimeAction.ReuseWorkspace);

    public int ReuseSharedCount => Entries.Count(e => e.Action == WorkspaceRuntimeAction.ReuseShared);

    public string Summary
    {
        get
        {
            if (!CanStart)
            {
                return $"BLOCKED · {IssueCount} issue(s) · no mutation occurred.";
            }

            return $"VALID · isolate {IsolatedCount} · reuse shared {ReuseSharedCount}.";
        }
    }
}

public sealed class WorkspaceRuntimeExecutionResult
{
    public bool Succeeded { get; init; }

    public bool Blocked { get; init; }

    public string? Error { get; init; }

    public WorkspaceRuntimePlan? Plan { get; init; }

    public SharedRuntimeStatus Status { get; init; }

    public static WorkspaceRuntimeExecutionResult Ok(WorkspaceRuntimePlan? plan, SharedRuntimeStatus status)
        => new() { Succeeded = true, Plan = plan, Status = status };

    public static WorkspaceRuntimeExecutionResult Fail(string error, WorkspaceRuntimePlan? plan = null, SharedRuntimeStatus status = SharedRuntimeStatus.Failed)
        => new() { Succeeded = false, Error = error, Plan = plan, Status = status };

    public static WorkspaceRuntimeExecutionResult Block(string error, WorkspaceRuntimePlan? plan = null)
        => new() { Succeeded = false, Blocked = true, Error = error, Plan = plan, Status = SharedRuntimeStatus.Blocked };
}

public sealed class WorkspaceRuntimeSession
{
    public string SessionId { get; set; } = string.Empty;

    public string? WorkspaceId { get; set; }

    public string? ProfileId { get; set; }

    public string? AgentSessionId { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    public SharedRuntimeStatus Status { get; set; } = SharedRuntimeStatus.Stopped;

    public HashSet<string> OwnedServiceNames { get; } = new(StringComparer.OrdinalIgnoreCase);

    public string? StartedHead { get; set; }
}
