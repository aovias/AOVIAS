using System.IO.Compression;
using Aovias.Publishing.Contracts;
using Aovias.Publishing.Core.Packaging;
using Xunit;

namespace Aovias.Publishing.Core.Tests;

public sealed class PublishingStagingArtifactsTests
{
    [Fact]
    public void EnsureServiceImageTarInStaging_restores_from_protected_tar_when_staging_tar_removed()
    {
        var workspace = Path.Combine(Path.GetTempPath(), "staging-artifacts-" + Guid.NewGuid().ToString("N"));
        var staging = Path.Combine(workspace, "staging");
        var protectedDir = Path.Combine(workspace, "protected");
        Directory.CreateDirectory(staging);
        Directory.CreateDirectory(protectedDir);

        var packagePath = Path.Combine(workspace, "service-1.0.0.aovdeploy");
        var stagingTar = Path.Combine(staging, DeployPackageLayout.ServiceImageTar);
        var protectedTar = Path.Combine(protectedDir, DeployPackageLayout.ServiceImageTar);
        File.WriteAllText(protectedTar, "docker-image-bytes");
        File.WriteAllText(stagingTar, "docker-image-bytes");
        ZipFile.CreateFromDirectory(staging, packagePath);
        File.Delete(stagingTar);

        var pending = new PendingPublishValidation
        {
            ServiceId = "service",
            ProjectPath = "service.csproj",
            Version = "1.0.0",
            Profile = "local",
            Publisher = "tester",
            StagingDirectory = staging,
            WorkingDirectory = workspace,
            PackagePath = packagePath,
            ServiceImageTarPath = protectedTar,
            ContainerPort = 8080
        };

        try
        {
            PublishingStagingArtifacts.EnsureServiceImageTarInStaging(pending);
            Assert.True(File.Exists(stagingTar));
            Assert.True(new FileInfo(stagingTar).Length > 0);
        }
        finally
        {
            if (Directory.Exists(workspace))
            {
                Directory.Delete(workspace, recursive: true);
            }
        }
    }
}
