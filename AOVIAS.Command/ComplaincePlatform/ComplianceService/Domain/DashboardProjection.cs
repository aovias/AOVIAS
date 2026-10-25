namespace ComplianceService.Domain;

/// <summary>
/// Event-maintained dashboard read model for a tenant/hospital/framework scope.
/// Updated incrementally by <see cref="Application.Dashboard.Projection.DashboardProjectionEngine"/>.
/// </summary>
public class DashboardProjection
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    /// <summary>Hospital scope. Guid.Empty when projection covers all hospitals in tenant.</summary>
    public Guid HospitalId { get; set; }

    /// <summary>Framework scope. Guid.Empty when projection covers all frameworks for the hospital.</summary>
    public Guid FrameworkId { get; set; }

    /// <summary>Schema version for payload migration (<see cref="Application.Dashboard.Projection.DashboardProjectionPayload"/>).</summary>
    public int ProjectionSchemaVersion { get; set; } = 1;

    /// <summary>Monotonic data version bumped on each successful update.</summary>
    public long DataVersion { get; set; }

    public bool IsComplete { get; set; }

    public bool IsCorrupted { get; set; }

    public string? CorruptionReason { get; set; }

    // Denormalized KPI columns for fast summary reads (owned by projection engine).
    public decimal OverallComplianceScore { get; set; }

    public decimal AuditConfidenceScore { get; set; }

    public int ActiveJourneysCount { get; set; }

    public int TotalJourneysCount { get; set; }

    public int PendingTasksCount { get; set; }

    public int OverdueTasksCount { get; set; }

    public int OpenAlertsCount { get; set; }

    public DateTime? LastActivityAt { get; set; }

    public DateTime? ScoreCalculatedAt { get; set; }

    /// <summary>Serialized <see cref="Application.Dashboard.Projection.DashboardProjectionPayload"/>.</summary>
    public string PayloadJson { get; set; } = "{}";

    public DateTime LastUpdatedAt { get; set; }

    public DateTime? LastEventProcessedAt { get; set; }

    public string? LastEventType { get; set; }

    public Guid? LastProcessedOutboxMessageId { get; set; }

    public DateTime CreatedAt { get; set; }
}
