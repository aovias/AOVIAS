namespace ComplianceService.Application.RoleExperience;

/// <summary>
/// Best-effort role-pack engagement counters (Phase 3 Sprint 6).
/// Derived from existing analytics / operational metrics — not a new event store.
/// </summary>
public sealed class RoleEngagementDto
{
    public required string PackId { get; init; }
    public required string Persona { get; init; }
    public RoleEngagementCountersDto Counters { get; init; } = new();
    public DateTimeOffset GeneratedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public string Source { get; init; } = "analytics-operational-best-effort";
}

public sealed class RoleEngagementCountersDto
{
    /// <summary>Completed tasks from task throughput summary.</summary>
    public int TasksCompleted { get; init; }

    /// <summary>Evidence items approved (proxy for completed reviews).</summary>
    public int ReviewsCompleted { get; init; }

    /// <summary>Pending evidence / CAPA reviews still open.</summary>
    public int ReviewsPending { get; init; }

    /// <summary>Closed deficiencies (proxy for CAPA resolutions).</summary>
    public int CapaResolutions { get; init; }

    /// <summary>Effective CAPAs when available.</summary>
    public int CapasEffective { get; init; }

    /// <summary>Audit sessions closed or submitted.</summary>
    public int AuditsCompleted { get; init; }

    /// <summary>Open overdue tasks (attention, not completion).</summary>
    public int OverdueTasks { get; init; }

    /// <summary>Open overdue CAPAs (attention).</summary>
    public int OverdueCapas { get; init; }
}
