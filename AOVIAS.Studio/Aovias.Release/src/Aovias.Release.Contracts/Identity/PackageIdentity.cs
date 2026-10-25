namespace Aovias.Release.Contracts.Identity;

/// <summary>
/// Immutable package identity assigned when a package is first created.
/// </summary>
public sealed class PackageIdentity
{
    /// <summary>
    /// Gets or sets the immutable package identifier (for example PKG-7F4A9C21).
    /// </summary>
    public required string PackageId { get; init; }

    /// <summary>
    /// Gets or sets the semantic package version.
    /// </summary>
    public required string PackageVersion { get; init; }

    /// <summary>
    /// Gets or sets the application name the package belongs to.
    /// </summary>
    public required string ApplicationName { get; init; }

    /// <summary>
    /// Gets or sets the service identifier within the application.
    /// </summary>
    public required string ServiceId { get; init; }
}
