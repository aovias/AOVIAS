namespace Aovias.Release.Contracts.Packages;

/// <summary>
/// Parsed contents of an AOVIAS .aovpkg archive.
/// </summary>
public sealed class PackageContents
{
    public required string PackagePath { get; init; }

    public required PackageManifest Manifest { get; init; }

    public required ServiceDescription Service { get; init; }

    public required DeploymentDescription Deployment { get; init; }

    public required HealthDescription Health { get; init; }

    public required string ReleaseNotes { get; init; }

    public required IReadOnlyDictionary<string, string> Checksums { get; init; }

    public string? Signature { get; init; }

    public IReadOnlyList<string> ArtifactPaths { get; init; } = [];

    public bool HasServiceImage { get; init; }

    public string PackageFormat { get; init; } = ".aovpkg";
}
