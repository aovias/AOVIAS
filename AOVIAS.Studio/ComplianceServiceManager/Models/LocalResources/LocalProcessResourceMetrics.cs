namespace ComplianceServiceManager.Models.LocalResources;

/// <summary>Per-process local resource sample for Studio-associated or arbitrary PIDs.</summary>
public sealed class LocalProcessResourceMetrics
{
    public int ProcessId { get; init; }

    public string ProcessName { get; init; } = string.Empty;

    /// <summary>
    /// Process CPU as a percentage of total machine capacity (0–100), measured over the sampling interval.
    /// Null when warm-up sample is required or the process cannot be read.
    /// </summary>
    public double? CpuPercent { get; init; }

    public long? MemoryWorkingSetBytes { get; init; }

    public DateTimeOffset? StartTime { get; init; }

    public bool IsAccessible { get; init; }

    public string? Error { get; init; }
}
