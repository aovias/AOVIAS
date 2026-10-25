namespace Aovias.Release.Contracts.Packages;

/// <summary>
/// Package dependency metadata.
/// </summary>
public sealed class PackageDependency
{
    public required string PackageId { get; init; }

    public required string PackageVersion { get; init; }

    public required string DisplayName { get; init; }

    public bool Required { get; init; } = true;
}
