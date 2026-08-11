using System.Security.Cryptography;
using Aovias.Publishing.Contracts;
using Aovias.Release.Contracts;
using Aovias.Release.Contracts.Packages;

namespace Aovias.PackageBuilder.Services;

/// <summary>
/// Reads both legacy <c>.aovpkg</c> and APE <c>.aovdeploy</c> packages into release contracts.
/// </summary>
public sealed class UnifiedPackageReader : Abstractions.IPackageReader
{
    private readonly PackageArchiveReader legacyReader;
    private readonly IDeployPackageReader deployPackageReader;

    public UnifiedPackageReader(IDeployPackageReader deployPackageReader)
    {
        this.deployPackageReader = deployPackageReader;
        legacyReader = new PackageArchiveReader();
    }

    /// <inheritdoc />
    public async Task<PackageContents> ReadAsync(string packagePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packagePath);

        if (packagePath.EndsWith(DeployPackageLayout.Extension, StringComparison.OrdinalIgnoreCase))
        {
            var deployContents = await deployPackageReader.ReadAsync(packagePath, cancellationToken)
                ?? throw new InvalidOperationException("Unable to parse deployment package.");

            return MapDeployPackage(packagePath, deployContents);
        }

        if (packagePath.EndsWith(PackageLayout.Extension, StringComparison.OrdinalIgnoreCase))
        {
            return await legacyReader.ReadAsync(packagePath, cancellationToken);
        }

        throw new InvalidOperationException("Package must use .aovpkg or .aovdeploy extension.");
    }

    private static PackageContents MapDeployPackage(string packagePath, DeployPackageContents deployContents)
    {
        var manifest = deployContents.Manifest;
        var metadata = deployContents.Metadata;
        var health = deployContents.Health;
        var artifactPaths = new List<string>();

        if (deployContents.HasServiceImage)
        {
            artifactPaths.Add(DeployPackageLayout.ServiceImageTar);
        }

        if (!string.IsNullOrWhiteSpace(deployContents.ComposeContent))
        {
            artifactPaths.Add(DeployPackageLayout.ComposeFile);
        }

        return new PackageContents
        {
            PackagePath = packagePath,
            PackageFormat = DeployPackageLayout.Extension,
            HasServiceImage = deployContents.HasServiceImage,
            Manifest = new PackageManifest
            {
                PackageId = manifest.Service,
                PackageVersion = manifest.Version,
                ApplicationName = manifest.Service,
                ServiceId = manifest.Service,
                DisplayName = manifest.Service,
                RuntimeVersion = manifest.Version,
                PlatformVersion = metadata.PublishingEngineVersion,
                Framework = manifest.Type,
                BuildNumber = metadata.CreatedUtc.ToString("yyyyMMddHHmmss"),
                CreatedAt = metadata.CreatedUtc,
                CreatedBy = metadata.Publisher,
                PackageHash = ComputePackageHash(packagePath),
                MinimumRuntimeVersion = manifest.Version,
                SupportedOperatingSystems = ["linux"]
            },
            Service = new ServiceDescription
            {
                ServiceId = manifest.Service,
                DisplayName = manifest.Service,
                ContainerName = manifest.Container,
                ComposeFile = DeployPackageLayout.ComposeFile,
                HealthEndpoint = health.Endpoint,
                StartupOrder = 1,
                Category = manifest.Type,
                Description = $"AOVIAS deployment package for {manifest.Service} {manifest.Version}."
            },
            Deployment = new DeploymentDescription
            {
                ActivationMode = "Immediate",
                RequiresRestart = true,
                RollbackSupported = true,
                EstimatedDeploymentTime = TimeSpan.FromMinutes(5),
                EstimatedDowntime = TimeSpan.FromMinutes(1),
                DeploymentStrategy = "OfflineImage"
            },
            Health = new HealthDescription
            {
                HealthEndpoint = health.Endpoint,
                ExpectedStatusCode = health.ExpectedStatus,
                Timeout = TimeSpan.FromSeconds(health.TimeoutSeconds),
                RetryCount = health.Retries,
                FailureThreshold = 1
            },
            ReleaseNotes = $"Published by {metadata.Publisher} on {metadata.CreatedUtc:u}.",
            Checksums = deployContents.Checksums,
            ArtifactPaths = artifactPaths
        };
    }

    private static string ComputePackageHash(string packagePath)
    {
        using var stream = File.OpenRead(packagePath);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }
}
