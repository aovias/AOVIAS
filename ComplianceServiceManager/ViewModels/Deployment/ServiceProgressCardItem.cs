namespace ComplianceServiceManager.ViewModels.Deployment;

/// <summary>
/// Per-service deployment progress card (Sprint 12 D6).
/// </summary>
public sealed class ServiceProgressCardItem
{
    public required string ServiceId { get; init; }

    public required string Name { get; init; }

    public string Phase { get; set; } = "Waiting";

    public int ProgressPercent { get; set; }

    public string HealthStatus { get; set; } = "Unknown";

    public string CertificationStatus { get; set; } = "Pending";

    public string DurationDisplay { get; set; } = "00:00";
}
