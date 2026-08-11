using System.IO.Compression;
using System.Text.Json;
using Aovias.Publishing.Contracts;
using Aovias.Publishing.Core.Orchestration;
using Xunit;

namespace Aovias.Publishing.Core.Tests;

public sealed class SolutionPackageInspectorTests
{
    [Fact]
    public async Task InspectReleaseLibraryAsync_uses_manifest_package_paths_not_stale_category_copies()
    {
        var root = Path.Combine(Path.GetTempPath(), $"inspect-{Guid.NewGuid():N}");
        var freshPackage = Path.Combine(root, "runtime-agent.aovdeploy");
        var staleCategoryDirectory = Path.Combine(root, "Runtime");
        var stalePackage = Path.Combine(staleCategoryDirectory, "runtime-agent.aovdeploy");

        Directory.CreateDirectory(staleCategoryDirectory);
        CreateMinimalPackage(stalePackage, includeCertificate: false);
        CreateMinimalPackage(freshPackage, includeCertificate: true);

        var manifest = new SolutionDeploymentManifest
        {
            Profile = "local",
            Version = "1.0.0",
            GeneratedUtc = DateTimeOffset.UtcNow,
            StartupOrder = ["runtime-agent"],
            Services =
            [
                new SolutionDeploymentServiceEntry
                {
                    ServiceId = "runtime-agent",
                    Name = "Runtime Agent",
                    Version = "1.0.0",
                    StartupOrder = 10,
                    HealthEndpoint = "/health",
                    DockerImage = "aovias/runtime-agent:1.0.0",
                    PackageFileName = "runtime-agent.aovdeploy",
                    PackagePath = freshPackage,
                    DeploymentReady = true
                }
            ]
        };

        File.WriteAllText(
            Path.Combine(root, "solution-deployment.json"),
            JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));

        var inspector = new SolutionPackageInspector(new AlwaysValidVerifier());
        var result = await inspector.InspectReleaseLibraryAsync(root, profile: "local");

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Single(result.Packages);
        Assert.Equal(freshPackage, result.Packages[0].PackagePath, ignoreCase: true);
    }

    private static void CreateMinimalPackage(string packagePath, bool includeCertificate)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(packagePath)!);
        using var archive = ZipFile.Open(packagePath, ZipArchiveMode.Create);
        foreach (var required in new[]
                 {
                     DeployPackageLayout.ServiceImageTar,
                     DeployPackageLayout.ComposeFile,
                     DeployPackageLayout.EnvTemplateFile,
                     DeployPackageLayout.ManifestFile,
                     DeployPackageLayout.MetadataFile,
                     DeployPackageLayout.ChecksumsFile,
                     DeployPackageLayout.HealthFile,
                     DeployPackageLayout.DeploymentReadinessCertificateFile
                 })
        {
            if (!includeCertificate && required == DeployPackageLayout.DeploymentReadinessCertificateFile)
            {
                continue;
            }

            var entry = archive.CreateEntry(required);
            using var writer = new StreamWriter(entry.Open());
            writer.Write("{}");
        }
    }

    private sealed class AlwaysValidVerifier : IDeployPackageVerifier
    {
        public Task<DeployPackageVerificationResult> VerifyAsync(
            string packagePath,
            bool expectCertificate,
            CancellationToken cancellationToken = default)
            => Task.FromResult(DeployPackageVerificationResult.Success(
                DeployPackageLayout.RequiredFiles,
                DeployPackageLayout.RequiredFiles.Count,
                expectCertificate));
    }
}
