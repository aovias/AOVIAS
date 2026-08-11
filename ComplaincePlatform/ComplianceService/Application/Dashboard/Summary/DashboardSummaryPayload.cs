namespace ComplianceService.Application.Dashboard.Summary;

/// <summary>
/// Internal lightweight read model for Phase 1 dashboard KPIs (PRAMAN experience path).
/// </summary>
public sealed class DashboardSummaryPayload
{
    public Guid OrganizationId { get; init; }

    public Guid? HospitalId { get; init; }

    public Guid? FrameworkId { get; init; }

    public decimal OverallCompliancePercent { get; init; }

    public int ActiveJourneysCount { get; init; }

    public int PendingTasks { get; init; }

    public int OutstandingTasks => PendingTasks;

    public int AlertsCount { get; init; }

    public int NotificationsCount { get; init; }

    public int HospitalsSummaryCount { get; init; }

    public bool RecentActivityIndicator { get; init; }

    public DateTime? LastActivityAt { get; init; }

    public DateTimeOffset LastUpdated { get; init; }

    public DashboardSummarySource Source { get; init; }
}

public enum DashboardSummarySource
{
    Snapshot,
    LiveFallback,
}
