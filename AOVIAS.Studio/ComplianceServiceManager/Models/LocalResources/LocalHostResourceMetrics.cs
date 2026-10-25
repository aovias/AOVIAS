namespace ComplianceServiceManager.Models.LocalResources;

/// <summary>
/// Local developer machine host metrics (not Runtime Agent / remote host metrics).
/// </summary>
public sealed class LocalHostResourceMetrics
{
    public double? CpuPercent { get; init; }

    public long MemoryTotalBytes { get; init; }

    public long MemoryUsedBytes { get; init; }

    public long MemoryAvailableBytes { get; init; }

    public double? MemoryPercent { get; init; }

    public string DiskRootPath { get; init; } = string.Empty;

    public long DiskTotalBytes { get; init; }

    public long DiskUsedBytes { get; init; }

    public long DiskFreeBytes { get; init; }

    public double? DiskPercent { get; init; }

    public LocalResourceStatus CpuStatus { get; init; } = LocalResourceStatus.Unknown;

    public LocalResourceStatus MemoryStatus { get; init; } = LocalResourceStatus.Unknown;

    public LocalResourceStatus DiskStatus { get; init; } = LocalResourceStatus.Unknown;

    public string? Notes { get; init; }
}
