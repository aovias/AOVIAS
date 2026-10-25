namespace Aovias.Runtime.Contracts.Monitoring;

/// <summary>
/// Represents host resource statistics for runtime monitoring.
/// </summary>
public sealed class RuntimeStatisticsDto
{
    /// <summary>
    /// Gets or sets the disk usage percentage.
    /// </summary>
    public double DiskUsagePercent { get; init; }

    /// <summary>
    /// Gets or sets the CPU usage percentage.
    /// </summary>
    public double CpuUsagePercent { get; init; }

    /// <summary>
    /// Gets or sets the memory usage percentage.
    /// </summary>
    public double MemoryUsagePercent { get; init; }

    /// <summary>
    /// Gets or sets when the statistics were collected.
    /// </summary>
    public DateTimeOffset CollectedAt { get; init; }

    /// <summary>
    /// Gets or sets the operating system description.
    /// </summary>
    public string OperatingSystem { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the kernel version.
    /// </summary>
    public string Kernel { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the host name.
    /// </summary>
    public string Hostname { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the deployment environment name.
    /// </summary>
    public string Environment { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the public URL for the runtime.
    /// </summary>
    public string PublicUrl { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets Docker overview metrics.
    /// </summary>
    public DockerOverviewDto Docker { get; init; } = new();
}
