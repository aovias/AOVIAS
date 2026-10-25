namespace Aovias.Runtime.Agent.Domain.Entities;

/// <summary>
/// Domain entity representing the canonical runtime manifest persisted on the host.
/// </summary>
public sealed class RuntimeManifestEntity
{
    /// <summary>
    /// Gets or sets the runtime agent version.
    /// </summary>
    public string RuntimeVersion { get; set; } = "1.0";

    /// <summary>
    /// Gets or sets the deployment environment name.
    /// </summary>
    public string Environment { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the host name.
    /// </summary>
    public string Hostname { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the server display name.
    /// </summary>
    public string ServerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique server identifier.
    /// </summary>
    public string ServerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the AOVIAS platform version.
    /// </summary>
    public string PlatformVersion { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the deployment agent version.
    /// </summary>
    public string DeploymentAgentVersion { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Docker network name.
    /// </summary>
    public string DockerNetwork { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the runtime was started.
    /// </summary>
    public DateTimeOffset StartedAt { get; set; }

    /// <summary>
    /// Gets or sets when the manifest was last updated.
    /// </summary>
    public DateTimeOffset LastUpdated { get; set; }

    /// <summary>
    /// Gets or sets infrastructure services registered in the manifest.
    /// </summary>
    public List<InfrastructureServiceEntity> InfrastructureServices { get; set; } = [];

    /// <summary>
    /// Gets or sets application services registered in the manifest.
    /// </summary>
    public List<ApplicationServiceEntity> ApplicationServices { get; set; } = [];
}

/// <summary>
/// Domain entity describing an infrastructure service entry in the manifest.
/// </summary>
public sealed class InfrastructureServiceEntity
{
    /// <summary>
    /// Gets or sets the service identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the service display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the service category.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the deployed version.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the operational status label.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the health label.
    /// </summary>
    public string Health { get; set; } = string.Empty;
}

/// <summary>
/// Domain entity describing an application service entry in the manifest.
/// </summary>
public sealed class ApplicationServiceEntity
{
    /// <summary>
    /// Gets or sets the service identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the service display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the service category.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the deployed version.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the operational status label.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the health label.
    /// </summary>
    public string Health { get; set; } = string.Empty;
}
