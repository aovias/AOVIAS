using System.IO.Compression;
using Aovias.Publishing.Contracts;
using Aovias.Publishing.Core.Common;

namespace Aovias.Publishing.Core.Packaging;

/// <summary>
/// Preserves publishing staging artifacts across deferred DRV and final packaging.
/// </summary>
internal static class PublishingStagingArtifacts
{
    internal static string PrepareProtectedServiceImageTar(PublishingContext context)
    {
        var sourceTar = ResolveServiceImageTar(context);
        var protectedDirectory = Path.Combine(context.WorkingDirectory!, "protected");
        Directory.CreateDirectory(protectedDirectory);

        var protectedTar = Path.Combine(protectedDirectory, DeployPackageLayout.ServiceImageTar);
        File.Copy(sourceTar, protectedTar, overwrite: true);
        return protectedTar;
    }

    internal static void EnsureServiceImageTarInStaging(PendingPublishValidation pending)
    {
        var stagingTar = Path.Combine(pending.StagingDirectory, DeployPackageLayout.ServiceImageTar);
        if (File.Exists(stagingTar) && new FileInfo(stagingTar).Length > 0)
        {
            return;
        }

        if (File.Exists(pending.ServiceImageTarPath) && new FileInfo(pending.ServiceImageTarPath).Length > 0)
        {
            Directory.CreateDirectory(pending.StagingDirectory);
            File.Copy(pending.ServiceImageTarPath, stagingTar, overwrite: true);
            return;
        }

        throw new InvalidOperationException(
            "Cannot restore service-image.tar because the protected Docker image export is missing. " +
            "Rebuild the Runtime Agent image instead of reusing a stale deployment package.");
    }

    internal static void EnsureServiceImageTarInStaging(PublishingContext context)
    {
        EnsureServiceImageTarInStaging(new PendingPublishValidation
        {
            ServiceId = context.Manifest?.Service ?? context.Request.ServiceId ?? "service",
            ProjectPath = context.Request.ProjectPath,
            Version = context.Request.Version ?? PublishingVersionPolicy.DefaultPackageVersion,
            Profile = context.Request.Profile,
            Publisher = context.Request.Publisher,
            StagingDirectory = context.StagingDirectory ?? throw new InvalidOperationException("Staging directory is required."),
            WorkingDirectory = context.WorkingDirectory ?? throw new InvalidOperationException("Working directory is required."),
            PackagePath = context.PackagePath ?? context.InterimPackagePath ?? throw new InvalidOperationException("Package path is required."),
            ServiceImageTarPath = context.DockerImageTarPath ?? Path.Combine(context.StagingDirectory!, DeployPackageLayout.ServiceImageTar),
            ContainerPort = context.Discovery?.ContainerPort ?? 8080
        });
    }

    private static string ResolveServiceImageTar(PublishingContext context)
    {
        var stagingTar = Path.Combine(context.StagingDirectory!, DeployPackageLayout.ServiceImageTar);
        if (File.Exists(stagingTar) && new FileInfo(stagingTar).Length > 0)
        {
            return stagingTar;
        }

        if (!string.IsNullOrWhiteSpace(context.DockerImageTarPath)
            && File.Exists(context.DockerImageTarPath)
            && new FileInfo(context.DockerImageTarPath).Length > 0)
        {
            return context.DockerImageTarPath;
        }

        throw new InvalidOperationException("Docker image tar was not found in the publishing workspace.");
    }
}
