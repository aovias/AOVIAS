using Aovias.Runtime.Contracts.Monitoring;

namespace Aovias.Runtime.Agent.Application.Interfaces;

/// <summary>
/// Provides read-only Docker engine inspection capabilities.
/// </summary>
public interface IDockerProvider
{
    /// <summary>
    /// Retrieves Docker overview metrics.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>Docker overview metrics.</returns>
    Task<DockerOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Provides host and runtime statistics for monitoring dashboards.
/// </summary>
public interface IStatisticsProvider
{
    /// <summary>
    /// Retrieves the current runtime statistics snapshot.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>Runtime statistics snapshot.</returns>
    Task<RuntimeStatisticsDto> GetStatisticsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Host system metrics snapshot.
/// </summary>
public sealed class SystemMetricsSnapshot
{
    /// <summary>
    /// Gets or sets the CPU usage percentage.
    /// </summary>
    public double CpuUsagePercent { get; init; }

    /// <summary>
    /// Gets or sets the memory usage percentage.
    /// </summary>
    public double MemoryUsagePercent { get; init; }

    /// <summary>
    /// Gets or sets the disk usage percentage.
    /// </summary>
    public double DiskUsagePercent { get; init; }

    /// <summary>
    /// Gets or sets the operating system description.
    /// </summary>
    public string OperatingSystem { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the kernel version.
    /// </summary>
    public string Kernel { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the machine name.
    /// </summary>
    public string MachineName { get; init; } = string.Empty;
}
