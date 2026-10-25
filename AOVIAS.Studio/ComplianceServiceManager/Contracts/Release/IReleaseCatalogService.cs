namespace ComplianceServiceManager.Contracts.Release;

/// <summary>
/// Release library entry discovered from the catalog and runtime repository.
/// </summary>
public sealed class ReleaseLibraryItem
{
    public required string Id { get; init; }

    public required string ServiceName { get; init; }

    public required string PackageId { get; init; }

    public required string PackageVersion { get; init; }

    public required string PackageSize { get; init; }

    public required string BuildNumber { get; init; }

    public required string ReleaseDate { get; init; }

    public required string EnvironmentCompatibility { get; init; }

    public required string DatabaseVersion { get; init; }

    public required string Status { get; init; }

    public required string PackageType { get; init; }

    public required string PackagePath { get; init; }

    public string? ReleaseId { get; init; }

    public string? ApplicationName { get; init; }

    public long PackageSizeBytes { get; init; }

    public DateTimeOffset SortDate { get; init; }

    public string? PackageHash { get; init; }

    public string? Publisher { get; init; }
}

/// <summary>
/// Discovers release packages for the Release Center library.
/// </summary>
public interface IReleaseCatalogService
{
    Task<IReadOnlyList<ReleaseLibraryItem>> DiscoverAsync(CancellationToken cancellationToken = default);
}
