namespace ComplianceServiceManager.Models.SharedRuntime;

/// <summary>Runtime-level Shared Runtime status. Service-level state remains with D2/ServiceManager.</summary>
public enum SharedRuntimeStatus
{
    Stopped = 0,
    Planned = 1,
    Validating = 2,
    Starting = 3,
    Running = 4,
    Degraded = 5,
    Stopping = 6,
    Failed = 7,
    Blocked = 8
}

/// <summary>Planned action for one profile member. Planning never mutates processes.</summary>
public enum SharedRuntimeAction
{
    Start = 0,
    Reuse = 1,
    Block = 2
}

public sealed class SharedRuntimeValidationIssue
{
    public required string Code { get; init; }

    public required string Message { get; init; }

    public string? ServiceName { get; init; }

    public string? InstanceId { get; init; }

    public int? Port { get; init; }

    public string? Phase { get; init; }
}

public sealed class SharedRuntimePlanEntry
{
    public string ServiceName { get; init; } = string.Empty;

    public string? InstanceId { get; init; }

    public string ScopeDisplay { get; init; } = "Shared";

    public int? Port { get; init; }

    public SharedRuntimeAction Action { get; init; }

    public string ActionDisplay { get; init; } = "START";

    public string? Reason { get; init; }

    public bool OwnedAfterStart { get; init; }

    public string OwnerDisplay { get; init; } = "—";
}

/// <summary>Read-only execution plan. No process mutation.</summary>
public sealed class SharedRuntimePlan
{
    public string ProfileId { get; init; } = string.Empty;

    public string ProfileName { get; init; } = string.Empty;

    public string ScopeDisplay { get; init; } = "Shared";

    public string CanonicalSource { get; init; } = string.Empty;

    public bool CanStart { get; init; }

    public IReadOnlyList<string> StartupOrder { get; init; } = [];

    public IReadOnlyList<SharedRuntimePlanEntry> Entries { get; init; } = [];

    public IReadOnlyList<SharedRuntimeValidationIssue> Issues { get; init; } = [];

    public int ServiceCount => Entries.Count;

    public int ToStartCount => Entries.Count(e => e.Action == SharedRuntimeAction.Start);

    public int ReuseCount => Entries.Count(e => e.Action == SharedRuntimeAction.Reuse);

    public int BlockedCount => Entries.Count(e => e.Action == SharedRuntimeAction.Block);

    public string Summary
    {
        get
        {
            if (!CanStart)
            {
                return $"BLOCKED · {BlockedCount} issue(s) · no mutation occurred.";
            }

            return $"VALID · {ServiceCount} services · start {ToStartCount} · reuse {ReuseCount}.";
        }
    }
}

public sealed class SharedRuntimeExecutionResult
{
    public bool Succeeded { get; init; }

    public bool Blocked { get; init; }

    public string? Error { get; init; }

    public SharedRuntimePlan? Plan { get; init; }

    public SharedRuntimeStatus Status { get; init; }

    public static SharedRuntimeExecutionResult Ok(SharedRuntimePlan? plan, SharedRuntimeStatus status)
        => new() { Succeeded = true, Plan = plan, Status = status };

    public static SharedRuntimeExecutionResult Fail(string error, SharedRuntimePlan? plan = null, SharedRuntimeStatus status = SharedRuntimeStatus.Failed)
        => new() { Succeeded = false, Error = error, Plan = plan, Status = status };

    public static SharedRuntimeExecutionResult Block(string error, SharedRuntimePlan? plan = null)
        => new() { Succeeded = false, Blocked = true, Error = error, Plan = plan, Status = SharedRuntimeStatus.Blocked };
}

/// <summary>In-memory Shared Runtime session. Does not persist CPU/RAM/process telemetry.</summary>
public sealed class SharedRuntimeSession
{
    public string SessionId { get; set; } = string.Empty;

    public string? ProfileId { get; set; }

    public string? ProfileName { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    public SharedRuntimeStatus Status { get; set; } = SharedRuntimeStatus.Stopped;

    public List<string> OwnedServiceNames { get; } = [];
}
