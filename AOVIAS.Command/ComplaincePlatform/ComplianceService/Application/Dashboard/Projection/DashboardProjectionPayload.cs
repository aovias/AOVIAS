using ComplianceService.Application.DTOs;

namespace ComplianceService.Application.Dashboard.Projection;

/// <summary>
/// Versioned dashboard projection payload. Field ownership is documented in
/// <see cref="DashboardProjectionFieldOwnership"/>.
/// </summary>
public sealed class DashboardProjectionPayload
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    /// <summary>Owned by: journey lifecycle events, snapshot recalc.</summary>
    public DashboardSnapshotSummaryDto Summary { get; set; } = new();

    /// <summary>Owned by: journey/score events.</summary>
    public List<FrameworkSnapshotDto> Frameworks { get; set; } = new();

    /// <summary>Owned by: journey/task events + org metadata hydration on read.</summary>
    public List<HospitalSnapshotDto> Hospitals { get; set; } = new();

    /// <summary>Owned by: task create/complete events.</summary>
    public TasksSummaryDto TasksSummary { get; set; } = new();

    /// <summary>Owned by: finding create/resolve events.</summary>
    public List<AlertSnapshotDto> Alerts { get; set; } = new();

    /// <summary>Owned by: snapshot recalc / score events.</summary>
    public List<ScoreTrendDto> ScoreTrends { get; set; } = new();

    /// <summary>Owned by: journey transition / expiry events.</summary>
    public List<MilestoneSnapshotDto> Milestones { get; set; } = new();

    /// <summary>Owned by: journey transition, evidence upload, assessment events.</summary>
    public List<ActivitySnapshotDto> RecentActivity { get; set; } = new();

    /// <summary>Owned by: journey lifecycle events.</summary>
    public Dictionary<string, int> JourneyStatusCounts { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Owned by: widget-specific incremental updates (extensibility).</summary>
    public Dictionary<string, DashboardWidgetAggregateDto> WidgetAggregates { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class DashboardWidgetAggregateDto
{
    public string WidgetKey { get; set; } = string.Empty;

    public decimal? NumericValue { get; set; }

    public int? CountValue { get; set; }

    public string? JsonValue { get; set; }

    public DateTime LastUpdatedAt { get; set; }
}
