namespace Aovias.Runtime.Contracts.Deployment;

/// <summary>
/// Placeholder contract for future deployment package metadata.
/// </summary>
public sealed class DeploymentPackageDto
{
    /// <summary>
    /// Gets or sets the package identifier.
    /// </summary>
    public string PackageId { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the package version.
    /// </summary>
    public string Version { get; init; } = string.Empty;
}
