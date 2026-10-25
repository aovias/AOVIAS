namespace Aovias.Runtime.Contracts.Runtime;

/// <summary>
/// Describes the runtime deployment environment.
/// </summary>
public sealed class RuntimeEnvironmentDto
{
    /// <summary>
    /// Gets or sets the environment name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the server name.
    /// </summary>
    public string ServerName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the server identifier.
    /// </summary>
    public string ServerId { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the host name.
    /// </summary>
    public string Hostname { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the Docker network name.
    /// </summary>
    public string DockerNetwork { get; init; } = string.Empty;
}
