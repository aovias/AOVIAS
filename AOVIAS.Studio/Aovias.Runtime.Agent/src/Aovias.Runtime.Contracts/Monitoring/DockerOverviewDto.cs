namespace Aovias.Runtime.Contracts.Monitoring;

/// <summary>
/// Docker engine overview metrics for monitoring dashboards.
/// </summary>
public sealed class DockerOverviewDto
{
    /// <summary>
    /// Gets or sets the Docker engine version.
    /// </summary>
    public string DockerVersion { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the total container count.
    /// </summary>
    public int ContainerCount { get; init; }

    /// <summary>
    /// Gets or sets the running container count.
    /// </summary>
    public int RunningContainerCount { get; init; }

    /// <summary>
    /// Gets or sets the image count.
    /// </summary>
    public int ImageCount { get; init; }

    /// <summary>
    /// Gets or sets the network count.
    /// </summary>
    public int NetworkCount { get; init; }

    /// <summary>
    /// Gets or sets the volume count.
    /// </summary>
    public int VolumeCount { get; init; }

    /// <summary>
    /// Gets or sets the engine status label.
    /// </summary>
    public string Status { get; init; } = string.Empty;
}
