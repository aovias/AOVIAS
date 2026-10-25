using System.Text.Json;
using Aovias.Release.Contracts;
using Aovias.Release.Contracts.Packages;
using Microsoft.Extensions.Logging;

namespace Aovias.PackageBuilder.Services;

/// <summary>
/// Builds AOVIAS .aovpkg release packages from published output.
/// </summary>
public sealed class PackageBuilderService : Abstractions.IPackageBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly Abstractions.IPackageIdentityGenerator identityGenerator;
    private readonly Abstractions.IManifestGenerator manifestGenerator;
    private readonly Abstractions.IChecksumProvider checksumProvider;
    private readonly Abstractions.IPackageValidator packageValidator;
    private readonly Abstractions.IPackageSigner packageSigner;
    private readonly ILogger<PackageBuilderService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PackageBuilderService"/> class.
    /// </summary>
    public PackageBuilderService(
        Abstractions.IPackageIdentityGenerator identityGenerator,
        Abstractions.IManifestGenerator manifestGenerator,
        Abstractions.IChecksumProvider checksumProvider,
        Abstractions.IPackageValidator packageValidator,
        Abstractions.IPackageSigner packageSigner,
        ILogger<PackageBuilderService> logger)
    {
        this.identityGenerator = identityGenerator;
        this.manifestGenerator = manifestGenerator;
        this.checksumProvider = checksumProvider;
        this.packageValidator = packageValidator;
        this.packageSigner = packageSigner;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<PackageBuildResult> CreatePackageAsync(PackageBuildRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!Directory.Exists(request.PublishedOutputPath))
        {
            throw new DirectoryNotFoundException($"Published output path was not found: {request.PublishedOutputPath}");
        }

        Directory.CreateDirectory(request.OutputDirectory);

        var packageId = string.IsNullOrWhiteSpace(request.PackageId)
            ? identityGenerator.GeneratePackageId()
            : request.PackageId;

        var stagingDirectory = Path.Combine(Path.GetTempPath(), "aovpkg-build", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(stagingDirectory);
        var artifactsDirectory = Path.Combine(stagingDirectory, "artifacts");
        Directory.CreateDirectory(artifactsDirectory);
        Directory.CreateDirectory(Path.Combine(stagingDirectory, "logs"));

        try
        {
            CopyPublishedOutput(request.PublishedOutputPath, artifactsDirectory);

            var service = request.Service ?? CreateDefaultService(request);
            var deployment = request.Deployment ?? CreateDefaultDeployment();
            var health = request.Health ?? CreateDefaultHealth(service.HealthEndpoint);
            var releaseNotes = request.ReleaseNotes ?? $"Release {request.PackageVersion} for {request.ApplicationName}.";

            await WriteJsonAsync(stagingDirectory, PackageLayout.ServiceFile, service, cancellationToken);
            await WriteJsonAsync(stagingDirectory, PackageLayout.DeploymentFile, deployment, cancellationToken);
            await WriteJsonAsync(stagingDirectory, PackageLayout.HealthFile, health, cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(stagingDirectory, PackageLayout.ReleaseNotesFile), releaseNotes, cancellationToken);

            var relativePaths = Directory.GetFiles(stagingDirectory, "*", SearchOption.AllDirectories)
                .Select(path => Path.GetRelativePath(stagingDirectory, path).Replace('\\', '/'))
                .Where(path => !path.Equals(PackageLayout.ManifestFile, StringComparison.OrdinalIgnoreCase) &&
                                !path.Equals(PackageLayout.ChecksumsFile, StringComparison.OrdinalIgnoreCase) &&
                                !path.Equals(PackageLayout.SignatureFile, StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var checksumContent = await checksumProvider.GenerateChecksumFileContentAsync(
                stagingDirectory,
                relativePaths,
                cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(stagingDirectory, PackageLayout.ChecksumsFile), checksumContent, cancellationToken);

            var packageFileName = $"{request.ApplicationName}-{request.PackageVersion}{PackageLayout.Extension}";
            var packagePath = Path.Combine(request.OutputDirectory, packageFileName);
            if (File.Exists(packagePath))
            {
                File.Delete(packagePath);
            }

            System.IO.Compression.ZipFile.CreateFromDirectory(stagingDirectory, packagePath, compressionLevel: System.IO.Compression.CompressionLevel.Optimal, includeBaseDirectory: false);
            var packageHash = await checksumProvider.ComputeFileHashAsync(packagePath, cancellationToken);
            var manifest = manifestGenerator.Generate(request, packageId, packageHash);
            await WriteJsonAsync(stagingDirectory, PackageLayout.ManifestFile, manifest, cancellationToken);

            await File.WriteAllTextAsync(
                Path.Combine(stagingDirectory, PackageLayout.SignatureFile),
                $"UNSIGNED::{packageId}::{DateTimeOffset.UtcNow:O}",
                cancellationToken);

            File.Delete(packagePath);
            System.IO.Compression.ZipFile.CreateFromDirectory(stagingDirectory, packagePath, compressionLevel: System.IO.Compression.CompressionLevel.Optimal, includeBaseDirectory: false);
            await packageSigner.SignAsync(packagePath, cancellationToken);

            logger.LogInformation("Created package {PackageId} at {PackagePath}", packageId, packagePath);

            return new PackageBuildResult
            {
                PackagePath = packagePath,
                Manifest = manifest,
                PackageHash = packageHash
            };
        }
        finally
        {
            try
            {
                Directory.Delete(stagingDirectory, recursive: true);
            }
            catch
            {
                // Best effort cleanup for staging directory.
            }
        }
    }

    /// <inheritdoc />
    public Task<PackageValidationResult> ValidatePackageAsync(string packagePath, CancellationToken cancellationToken = default)
        => packageValidator.ValidateAsync(packagePath, cancellationToken);

    private static void CopyPublishedOutput(string sourceDirectory, string destinationDirectory)
    {
        foreach (var file in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, file);
            var destinationPath = Path.Combine(destinationDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            File.Copy(file, destinationPath, overwrite: true);
        }
    }

    private static async Task WriteJsonAsync<T>(string root, string fileName, T value, CancellationToken cancellationToken)
    {
        var path = Path.Combine(root, fileName);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, value, JsonOptions, cancellationToken);
    }

    private static ServiceDescription CreateDefaultService(PackageBuildRequest request)
        => new()
        {
            ServiceId = request.ServiceId,
            DisplayName = request.DisplayName,
            ContainerName = request.ServiceId,
            ComposeFile = "docker-compose.yml",
            HealthEndpoint = "/health",
            StartupOrder = 100,
            Category = "Application",
            Description = request.DisplayName
        };

    private static DeploymentDescription CreateDefaultDeployment()
        => new()
        {
            ActivationMode = "Staged",
            RequiresRestart = true,
            RollbackSupported = true,
            EstimatedDeploymentTime = TimeSpan.FromMinutes(5),
            EstimatedDowntime = TimeSpan.FromMinutes(1),
            DeploymentStrategy = "Rolling"
        };

    private static HealthDescription CreateDefaultHealth(string healthEndpoint)
        => new()
        {
            HealthEndpoint = healthEndpoint,
            ExpectedStatusCode = 200,
            Timeout = TimeSpan.FromSeconds(30),
            RetryCount = 3,
            FailureThreshold = 2
        };
}
