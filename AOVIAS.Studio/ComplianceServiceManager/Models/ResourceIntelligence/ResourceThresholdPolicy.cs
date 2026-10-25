namespace ComplianceServiceManager.Models.ResourceIntelligence;

/// <summary>
/// Centralized advisory thresholds for D5 pressure classification.
/// Defaults are starting points only; they do not assert machine safety.
/// </summary>
public sealed class ResourceThresholdPolicy
{
    public double MemoryNormalMaxPercent { get; set; } = 70;

    public double MemoryWarningMaxPercent { get; set; } = 85;

    public double MemoryHighMaxPercent { get; set; } = 95;

    public double CpuNormalMaxPercent { get; set; } = 70;

    public double CpuWarningMaxPercent { get; set; } = 85;

    public double CpuHighMaxPercent { get; set; } = 95;

    public double DiskNormalMaxPercent { get; set; } = 70;

    public double DiskWarningMaxPercent { get; set; } = 85;

    public double DiskHighMaxPercent { get; set; } = 95;

    /// <summary>Minimum samples before using short-window CPU average (avoids single noisy sample).</summary>
    public int CpuWindowSampleCount { get; set; } = 3;

    public int HistoryCapacity { get; set; } = 20;

    public static ResourceThresholdPolicy Default { get; } = new();
}
