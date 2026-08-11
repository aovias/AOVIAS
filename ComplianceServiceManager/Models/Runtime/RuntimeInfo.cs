namespace ComplianceServiceManager.Models.Runtime;

/// <summary>
/// Describes the connected AOVIAS Runtime instance and its environment metadata.
/// </summary>
public sealed class RuntimeInfo
{
    /// <summary>
    /// Gets or sets the display name of the runtime.
    /// </summary>
    public string Name { get; init; } = "AOVIAS Runtime";

    /// <summary>
    /// Gets or sets the deployment environment name.
    /// </summary>
    public string Environment { get; init; } = "Demo";

    /// <summary>
    /// Gets or sets the server identifier.
    /// </summary>
    public string Server { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the runtime agent version.
    /// </summary>
    public string Version { get; init; } = "1.0";

    /// <summary>
    /// Gets or sets the overall runtime status label.
    /// </summary>
    public string Status { get; init; } = "Healthy";

    /// <summary>
    /// Gets or sets the overall runtime health classification.
    /// </summary>
    public RuntimeHealth Health { get; init; } = RuntimeHealth.Healthy;

    /// <summary>
    /// Gets or sets the timestamp when runtime metadata was last refreshed.
    /// </summary>
    public DateTimeOffset LastUpdated { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the host name reported by the runtime agent.
    /// </summary>
    public string Hostname { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the Docker network name used by the runtime stack.
    /// </summary>
    public string DockerNetwork { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the operating system description.
    /// </summary>
    public string OperatingSystem { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the kernel version reported by the host.
    /// </summary>
    public string Kernel { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the public IP address exposed by the runtime.
    /// </summary>
    public string PublicIp { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether SSL is enabled.
    /// </summary>
    public bool SslEnabled { get; init; }

    /// <summary>
    /// Gets or sets the HTTPS endpoint exposed by the runtime.
    /// </summary>
    public string HttpsEndpoint { get; init; } = string.Empty;
}
