using System.IO.Compression;
using System.Text.Json;
using Aovias.Publishing.Contracts;
using Aovias.Publishing.Core.Common;

namespace Aovias.Publishing.Core.Orchestration;

/// <summary>
/// Builds solution-level deployment manifests and demo bundles.
/// </summary>
public sealed class SolutionBundleBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public SolutionDeploymentManifest CreateSolutionDeploymentManifest(
        IReadOnlyList<SolutionProjectPublishResult> results,
        string profile,
        string version)
    {
        var services = results
            .Where(result => result.IsSuccess && !string.IsNullOrWhiteSpace(result.PackagePath))
            .OrderBy(result => result.Project.StartupOrder)
            .Select(result => new SolutionDeploymentServiceEntry
            {
                ServiceId = result.Project.ServiceId,
                Name = result.Project.Name,
                Version = result.Project.Version,
                StartupOrder = result.Project.StartupOrder,
                DependsOn = result.Project.DependsOn,
                ContainerPort = result.Project.ContainerPort,
                HealthEndpoint = result.Project.HealthEndpoint,
                DockerImage = $"{PublishingImageNaming.GetImageName(result.Project.ServiceId)}:{result.Project.Version}",
                PackageFileName = PublishingImageNaming.GetPackageFileName(result.Project.ServiceId),
                PackagePath = result.PackagePath,
                DeploymentReady = result.IsDeploymentReady
            })
            .ToList();

        return new SolutionDeploymentManifest
        {
            Profile = profile,
            Version = version,
            GeneratedUtc = DateTimeOffset.UtcNow,
            StartupOrder = services.Select(service => service.ServiceId).ToList(),
            Services = services
        };
    }

    public string WriteSolutionDeploymentManifest(SolutionDeploymentManifest manifest, string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        var path = Path.Combine(outputDirectory, "solution-deployment.json");
        File.WriteAllText(path, JsonSerializer.Serialize(manifest, JsonOptions));
        return path;
    }

    public string CreateDemoDeploymentBundle(
        SolutionDeploymentManifest manifest,
        IReadOnlyList<SolutionProjectPublishResult> results,
        string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        var bundlePath = Path.Combine(outputDirectory, "DemoDeployment.zip");
        if (File.Exists(bundlePath))
        {
            File.Delete(bundlePath);
        }

        using var archive = ZipFile.Open(bundlePath, ZipArchiveMode.Create);
        var manifestPath = WriteSolutionDeploymentManifest(manifest, outputDirectory);
        archive.CreateEntryFromFile(manifestPath, "solution-deployment.json", CompressionLevel.Optimal);

        foreach (var result in results.Where(result => result.IsSuccess && !string.IsNullOrWhiteSpace(result.PackagePath)))
        {
            var entryName = PublishingImageNaming.GetPackageFileName(result.Project.ServiceId);
            archive.CreateEntryFromFile(result.PackagePath!, entryName, CompressionLevel.Optimal);
        }

        return bundlePath;
    }
}

/// <summary>
/// Top-level deployment manifest for a published solution.
/// </summary>
public sealed class SolutionDeploymentManifest
{
    public required string Profile { get; init; }

    public required string Version { get; init; }

    public DateTimeOffset GeneratedUtc { get; init; }

    public IReadOnlyList<string> StartupOrder { get; init; } = [];

    public IReadOnlyList<SolutionDeploymentServiceEntry> Services { get; init; } = [];
}

/// <summary>
/// One deployable service entry inside <see cref="SolutionDeploymentManifest"/>.
/// </summary>
public sealed class SolutionDeploymentServiceEntry
{
    public required string ServiceId { get; init; }

    public required string Name { get; init; }

    public required string Version { get; init; }

    public int StartupOrder { get; init; }

    public IReadOnlyList<string> DependsOn { get; init; } = [];

    public int ContainerPort { get; init; }

    public required string HealthEndpoint { get; init; }

    public required string DockerImage { get; init; }

    public required string PackageFileName { get; init; }

    public string? PackagePath { get; init; }

    public bool DeploymentReady { get; init; }
}
