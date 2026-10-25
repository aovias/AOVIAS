namespace Aovias.Runtime.Contracts.Runtime;

/// <summary>
/// Describes runtime host information for discovery and operations.
/// </summary>
public sealed class RuntimeInfoDto
{
    /// <summary>
    /// Gets or sets the runtime display name.
    /// </summary>
    public string Name { get; init; } = "AOVIAS Runtime";

    /// <summary>
    /// Gets or sets the server identifier.
    /// </summary>
    public string Server { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the host name.
    /// </summary>
    public string Hostname { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the operating system description.
    /// </summary>
    public string OperatingSystem { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the kernel version.
    /// </summary>
    public string Kernel { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the public IP address.
    /// </summary>
    public string PublicIp { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether SSL is enabled.
    /// </summary>
    public bool SslEnabled { get; init; }

    /// <summary>
    /// Gets or sets the HTTPS endpoint.
    /// </summary>
    public string HttpsEndpoint { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the Docker network name.
    /// </summary>
    public string DockerNetwork { get; init; } = string.Empty;
}
