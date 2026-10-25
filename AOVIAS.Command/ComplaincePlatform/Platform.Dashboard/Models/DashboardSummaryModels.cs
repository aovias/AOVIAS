namespace Platform.Dashboard.Models;

/// <summary>
/// Lightweight organization snapshot for the platform dashboard summary.
/// </summary>
public sealed class DashboardOrganizationInfo
{
    public Guid OrganizationId { get; init; }

    public string? Name { get; init; }
}

/// <summary>
/// Active scope for the current dashboard session.
/// </summary>
public sealed class DashboardCurrentContext
{
    public Guid? HospitalId { get; init; }

    public string? HospitalName { get; init; }

    public Guid? FrameworkId { get; init; }

    public string? FrameworkName { get; init; }
}

/// <summary>
/// Aggregated health signal across contributing applications.
/// </summary>
public sealed class DashboardOverallHealth
{
    /// <summary>Healthy, Warning, Critical, Unknown</summary>
    public required string Status { get; init; }

    public string? Message { get; init; }

    public decimal? Score { get; init; }
}

/// <summary>
/// Partial summary contribution from a single dashboard provider.
/// </summary>
public sealed class DashboardSummaryContribution
{
    public DashboardApplicationInfo? Application { get; init; }

    public DashboardOverallHealth? OverallHealth { get; init; }

    public int? CriticalAlerts { get; init; }

    public int? UnreadNotifications { get; init; }

    public int? PendingTasks { get; init; }

    public DateTimeOffset? LastUpdated { get; init; }
}

/// <summary>
/// Merged platform dashboard summary. Lightweight only — no charts, heatmaps, or large collections.
/// </summary>
public sealed class DashboardSummaryResult
{
    public DashboardOrganizationInfo Organization { get; init; } = new() { OrganizationId = Guid.Empty };

    public DashboardCurrentContext CurrentContext { get; init; } = new();

    public IReadOnlyList<DashboardApplicationInfo> Applications { get; init; } = [];

    public DashboardOverallHealth OverallHealth { get; init; } = new() { Status = "Unknown" };

    public int CriticalAlerts { get; init; }

    public int UnreadNotifications { get; init; }

    public int PendingTasks { get; init; }

    public DateTimeOffset LastUpdated { get; init; } = DateTimeOffset.UtcNow;
}
