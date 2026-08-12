namespace ComplianceServiceManager.Models.LocalResources;

/// <summary>
/// Configurable visualization thresholds for local host resource status badges.
/// Values are percentages (0–100). They do not assert machine safety.
/// </summary>
public sealed class LocalResourceThresholds
{
    public double CpuWarningPercent { get; set; } = 80;

    public double CpuCriticalPercent { get; set; } = 95;

    public double MemoryWarningPercent { get; set; } = 80;

    public double MemoryCriticalPercent { get; set; } = 92;

    public double DiskWarningPercent { get; set; } = 85;

    public double DiskCriticalPercent { get; set; } = 95;

    public static LocalResourceThresholds Default { get; } = new();
}
