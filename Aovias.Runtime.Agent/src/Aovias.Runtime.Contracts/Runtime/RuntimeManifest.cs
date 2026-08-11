using Aovias.Runtime.Contracts.Services;

namespace Aovias.Runtime.Contracts.Runtime;

/// <summary>
/// Represents the canonical runtime manifest exposed by the control plane.
/// </summary>
public sealed class RuntimeManifest
{
    /// <summary>
    /// Gets or sets the runtime agent version.
    /// </summary>
    public string RuntimeVersion { get; init; } = "1.0";

    /// <summary>
    /// Gets or sets the deployment environment name.
    /// </summary>
    public string Environment { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the host name.
    /// </summary>
    public string Hostname { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the server display name.
    /// </summary>
    public string ServerName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique server identifier.
    /// </summary>
    public string ServerId { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the AOVIAS platform version.
    /// </summary>
    public string PlatformVersion { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the deployment agent version.
    /// </summary>
    public string DeploymentAgentVersion { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the Docker network name.
    /// </summary>
    public string DockerNetwork { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets when the runtime was started.
    /// </summary>
    public DateTimeOffset StartedAt { get; init; }

    /// <summary>
    /// Gets or sets when the manifest was last updated.
    /// </summary>
    public DateTimeOffset LastUpdated { get; init; }

    /// <summary>
    /// Gets or sets infrastructure services registered in the manifest.
    /// </summary>
    public IReadOnlyList<InfrastructureServiceDto> InfrastructureServices { get; init; } = [];

    /// <summary>
    /// Gets or sets application services registered in the manifest.
    /// </summary>
    public IReadOnlyList<ApplicationServiceDto> ApplicationServices { get; init; } = [];

    /// <summary>
    /// Gets or sets runtime capabilities advertised by the control plane.
    /// </summary>
    public IReadOnlyList<string> Capabilities { get; init; } = [];
}
