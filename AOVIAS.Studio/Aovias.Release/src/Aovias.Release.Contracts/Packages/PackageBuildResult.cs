namespace Aovias.Release.Contracts.Packages;

/// <summary>
/// Result of building an AOVIAS package.
/// </summary>
public sealed class PackageBuildResult
{
    public required string PackagePath { get; init; }

    public required PackageManifest Manifest { get; init; }

    public required string PackageHash { get; init; }
}
