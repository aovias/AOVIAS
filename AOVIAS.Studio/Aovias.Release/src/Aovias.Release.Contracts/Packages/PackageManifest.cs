namespace Aovias.Release.Contracts.Packages;

/// <summary>
/// Canonical manifest.json content for an AOVIAS package.
/// </summary>
public sealed class PackageManifest
{
    public required string PackageId { get; init; }

    public required string PackageVersion { get; init; }

    public required string ApplicationName { get; init; }

    public required string ServiceId { get; init; }

    public required string DisplayName { get; init; }

    public required string RuntimeVersion { get; init; }

    public required string PlatformVersion { get; init; }

    public required string Framework { get; init; }

    public required string BuildNumber { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required string CreatedBy { get; init; }

    public required string PackageHash { get; init; }

    public IReadOnlyList<PackageDependency> Dependencies { get; init; } = [];

    public required string MinimumRuntimeVersion { get; init; }

    public IReadOnlyList<string> SupportedOperatingSystems { get; init; } = [];
}
