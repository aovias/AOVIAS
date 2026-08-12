using System.IO;
using System.IO.Compression;
using System.Text.Json;
using Aovias.Configuration.Settings;
using Aovias.Publishing.Contracts;
using Aovias.Release.Contracts.Enums;
using ComplianceServiceManager.Contracts.Deployment;
using ComplianceServiceManager.Contracts.Release;
using Microsoft.Extensions.Options;

namespace ComplianceServiceManager.Services.Release;

/// <summary>
/// Combines local package discovery with runtime release repository entries.
/// </summary>
public sealed class ReleaseCatalogService : IReleaseCatalogService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IOptionsMonitor<AoviasPlatformOptions> platformOptions;
    private readonly IDeploymentService deploymentService;

    public ReleaseCatalogService(
        IOptionsMonitor<AoviasPlatformOptions> platformOptions,
        IDeploymentService deploymentService)
    {
        this.platformOptions = platformOptions;
        this.deploymentService = deploymentService;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReleaseLibraryItem>> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        var items = new Dictionary<string, ReleaseLibraryItem>(StringComparer.OrdinalIgnoreCase);
        var packagesRoot = Path.Combine(
            platformOptions.CurrentValue.ConfigRoot,
            "storage",
            "local",
            "packages");

        if (Directory.Exists(packagesRoot))
        {
            foreach (var packagePath in ResolveCanonicalPackagePaths(packagesRoot))
            {
                try
                {
                    var packageType = Path.GetExtension(packagePath);
                    await AddPackageToCatalogAsync(items, packagePath, packageType, cancellationToken);
                }
                catch
                {
                    // Skip unreadable or invalid local packages so the library still loads.
                }
            }
        }

        try
        {
            var releases = await deploymentService.GetReleasesAsync(cancellationToken);
            foreach (var release in releases)
            {
                var key = release.ReleaseId;
                if (items.Values.FirstOrDefault(item =>
                        string.Equals(item.PackageId, release.PackageId, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(item.PackageVersion, release.PackageVersion, StringComparison.OrdinalIgnoreCase)) is { } existing)
                {
                    items[existing.PackagePath] = CloneWithStatus(existing, release.ReleaseId, release.State.ToString());
                }
                else
                {
                    items[key] = new ReleaseLibraryItem
                    {
                        Id = release.ReleaseId,
                        ServiceName = release.ApplicationName,
                        ApplicationName = release.ApplicationName,
                        PackageId = release.PackageId,
                        PackageVersion = release.PackageVersion,
                        PackageSize = "-",
                        PackageSizeBytes = 0,
                        BuildNumber = "-",
                        ReleaseDate = release.UpdatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                        SortDate = release.UpdatedAt,
                        EnvironmentCompatibility = "Runtime",
                        DatabaseVersion = "See package",
                        Status = release.State.ToString(),
                        PackageType = ".aovpkg",
                        PackagePath = string.Empty,
                        ReleaseId = release.ReleaseId
                    };
                }
            }
        }
        catch
        {
            // Runtime agent may be offline; local packages remain available.
        }

        return items.Values
            .OrderByDescending(item => item.SortDate)
            .ToList();
    }

    internal static IEnumerable<string> ResolveCanonicalPackagePaths(string packagesRoot)
    {
        var resolved = new Dictionary<string, (string Path, DateTimeOffset SortDate)>(StringComparer.OrdinalIgnoreCase);

        var manifestPath = Path.Combine(packagesRoot, "solution-deployment.json");
        if (File.Exists(manifestPath))
        {
            try
            {
                var manifest = JsonSerializer.Deserialize<SolutionDeploymentManifestSnapshot>(
                    File.ReadAllText(manifestPath),
                    JsonOptions);

                if (manifest?.Services is { Count: > 0 })
                {
                    foreach (var service in manifest.Services)
                    {
                        if (string.IsNullOrWhiteSpace(service.PackagePath) || !File.Exists(service.PackagePath))
                        {
                            continue;
                        }

                        UpsertPackage(resolved, service.PackagePath, service.ServiceId);
                    }

                    return resolved.Values
                        .OrderByDescending(entry => entry.SortDate)
                        .Select(entry => entry.Path);
                }
            }
            catch
            {
                // Fall back to root-level discovery below.
            }
        }

        foreach (var pattern in new[] { "*.aovdeploy", "*.aovpkg" })
        {
            foreach (var packagePath in Directory.EnumerateFiles(packagesRoot, pattern, SearchOption.TopDirectoryOnly))
            {
                UpsertPackage(resolved, packagePath, Path.GetFileNameWithoutExtension(packagePath));
            }
        }

        return resolved.Values
            .OrderByDescending(entry => entry.SortDate)
            .Select(entry => entry.Path);
    }

    private static void UpsertPackage(
        Dictionary<string, (string Path, DateTimeOffset SortDate)> resolved,
        string packagePath,
        string serviceKey)
    {
        var sortDate = ResolvePackageSortDate(packagePath);
        if (resolved.TryGetValue(serviceKey, out var existing) && existing.SortDate >= sortDate)
        {
            return;
        }

        resolved[serviceKey] = (packagePath, sortDate);
    }

    private static DateTimeOffset ResolvePackageSortDate(string packagePath)
    {
        var fileTime = File.GetLastWriteTimeUtc(packagePath);
        try
        {
            using var archive = ZipFile.OpenRead(packagePath);
            var metadataEntry = archive.GetEntry(DeployPackageLayout.MetadataFile);
            if (metadataEntry is null)
            {
                return fileTime;
            }

            using var stream = metadataEntry.Open();
            using var document = JsonDocument.Parse(stream);
            if (document.RootElement.TryGetProperty("createdUtc", out var createdUtc)
                && DateTimeOffset.TryParse(createdUtc.GetString(), out var parsed))
            {
                return parsed > fileTime ? parsed : fileTime;
            }
        }
        catch
        {
            // Ignore unreadable metadata and use the file timestamp.
        }

        return fileTime;
    }

    private async Task AddPackageToCatalogAsync(
        Dictionary<string, ReleaseLibraryItem> items,
        string packagePath,
        string packageType,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var contents = await deploymentService.LoadPackageAsync(packagePath, cancellationToken);
        if (contents is null)
        {
            return;
        }

        var fileInfo = new FileInfo(packagePath);
        var sortDate = ResolvePackageSortDate(packagePath);
        var item = new ReleaseLibraryItem
        {
            Id = packagePath,
            ServiceName = contents.Manifest.DisplayName,
            ApplicationName = contents.Manifest.ApplicationName,
            PackageId = contents.Manifest.PackageId,
            PackageVersion = contents.Manifest.PackageVersion,
            PackageSize = FormatSize(fileInfo.Length),
            PackageSizeBytes = fileInfo.Length,
            BuildNumber = contents.Manifest.BuildNumber,
            ReleaseDate = sortDate.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
            SortDate = sortDate,
            EnvironmentCompatibility = string.Join(", ", contents.Manifest.SupportedOperatingSystems),
            DatabaseVersion = ResolveDatabaseVersion(contents.Deployment.RollbackSupported),
            Status = "Available",
            PackageType = packageType,
            PackagePath = packagePath,
            PackageHash = contents.Manifest.PackageHash,
            Publisher = contents.Manifest.CreatedBy
        };

        var serviceKey = Path.GetFileNameWithoutExtension(packagePath);
        if (items.TryGetValue(serviceKey, out var existing) && existing.SortDate >= item.SortDate)
        {
            return;
        }

        items[serviceKey] = item;
    }

    private static string FormatSize(long bytes)
    {
        if (bytes >= 1_048_576)
        {
            return $"{bytes / 1_048_576.0:F1} MB";
        }

        if (bytes >= 1024)
        {
            return $"{bytes / 1024.0:F1} KB";
        }

        return $"{bytes} B";
    }

    private static string ResolveDatabaseVersion(bool rollbackSupported)
        => rollbackSupported ? "Migrations included" : "Not specified";

    private static ReleaseLibraryItem CloneWithStatus(ReleaseLibraryItem source, string releaseId, string status)
        => new()
        {
            Id = source.Id,
            ServiceName = source.ServiceName,
            ApplicationName = source.ApplicationName,
            PackageId = source.PackageId,
            PackageVersion = source.PackageVersion,
            PackageSize = source.PackageSize,
            PackageSizeBytes = source.PackageSizeBytes,
            BuildNumber = source.BuildNumber,
            ReleaseDate = source.ReleaseDate,
            SortDate = source.SortDate,
            EnvironmentCompatibility = source.EnvironmentCompatibility,
            DatabaseVersion = source.DatabaseVersion,
            Status = status,
            PackageType = source.PackageType,
            PackagePath = source.PackagePath,
            ReleaseId = releaseId,
            PackageHash = source.PackageHash,
            Publisher = source.Publisher
        };

    private sealed class SolutionDeploymentManifestSnapshot
    {
        public IReadOnlyList<SolutionDeploymentServiceSnapshot> Services { get; init; } = [];
    }

    private sealed class SolutionDeploymentServiceSnapshot
    {
        public string ServiceId { get; init; } = string.Empty;

        public string? PackagePath { get; init; }

        public bool DeploymentReady { get; init; }
    }
}
