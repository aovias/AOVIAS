namespace ComplianceService.Application.Dashboard.Projection;

public sealed class DashboardProjectionTelemetry
{
    public long EventProcessingMs { get; set; }

    public long ProjectionUpdateMs { get; set; }

    public long ProjectionReadMs { get; set; }

    public bool UsedProjection { get; set; }

    public bool UsedFallback { get; set; }

    public int FallbackCount { get; set; }

    public DateTime? ProjectionLastUpdatedAt { get; set; }

    public string? LastEventType { get; set; }
}

public static class DashboardProjectionTelemetryKeys
{
    public const string HttpContextItemKey = "DashboardProjectionTelemetry";
}
