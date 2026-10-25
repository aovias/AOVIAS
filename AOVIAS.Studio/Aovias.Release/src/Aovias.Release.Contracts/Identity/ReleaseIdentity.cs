namespace Aovias.Release.Contracts.Identity;

/// <summary>
/// Deployment-specific release identity generated for each runtime deployment.
/// </summary>
public sealed class ReleaseIdentity
{
    /// <summary>
    /// Gets or sets the release identifier (for example REL-20260626-0001).
    /// </summary>
    public required string ReleaseId { get; init; }

    /// <summary>
    /// Gets or sets the immutable package identifier being deployed.
    /// </summary>
    public required string PackageId { get; init; }

    /// <summary>
    /// Gets or sets the package version being deployed.
    /// </summary>
    public required string PackageVersion { get; init; }

    /// <summary>
    /// Gets or sets the target runtime name.
    /// </summary>
    public required string Runtime { get; init; }

    /// <summary>
    /// Gets or sets the target environment name.
    /// </summary>
    public required string Environment { get; init; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the release was created.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }
}
