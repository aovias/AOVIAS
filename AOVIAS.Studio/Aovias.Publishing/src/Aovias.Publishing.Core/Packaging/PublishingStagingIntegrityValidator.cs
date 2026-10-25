using Aovias.Deployment.Contracts;
using Aovias.Publishing.Contracts;

namespace Aovias.Publishing.Core.Packaging;

/// <summary>
/// Validates staging directory integrity before packaging (M1.2 D5/D6).
/// </summary>
public static class PublishingStagingIntegrityValidator
{
    public static IReadOnlyList<string> Validate(PublishingContext context)
    {
        var errors = new List<string>();
        var staging = context.StagingDirectory;
        if (string.IsNullOrWhiteSpace(staging) || !Directory.Exists(staging))
        {
            errors.Add("Staging directory is missing.");
            return errors;
        }

        var identity = context.BuildIdentity;
        if (identity is null)
        {
            errors.Add("Publishing build identity is missing.");
            return errors;
        }

        var expectedFiles = BuildExpectedFiles(context);
        var actualFiles = Directory.EnumerateFiles(staging)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var expected in expectedFiles)
        {
            if (!actualFiles.Contains(expected))
            {
                errors.Add($"Missing staged artifact: {expected}");
            }
        }

        foreach (var actual in actualFiles)
        {
            if (IsAllowedOrphan(actual))
            {
                continue;
            }

            if (!expectedFiles.Contains(actual))
            {
                errors.Add($"Unexpected staged artifact: {actual}");
            }
        }

        if (context.Manifest is not null)
        {
            if (!string.Equals(context.Manifest.SourceRevision, identity.SourceRevision, StringComparison.Ordinal))
            {
                errors.Add("manifest.json source revision does not match the current build identity.");
            }

            if (!string.Equals(context.Manifest.PublishingInputFingerprint, identity.PublishingInputFingerprint, StringComparison.Ordinal))
            {
                errors.Add("manifest.json publishing input fingerprint does not match the current build identity.");
            }

            if (!string.IsNullOrWhiteSpace(identity.DockerImageIdentity)
                && !string.Equals(context.Manifest.DockerImageIdentity, identity.DockerImageIdentity, StringComparison.Ordinal))
            {
                errors.Add("manifest.json docker image identity does not match the current build identity.");
            }
        }

        ValidateArtifactFreshness(staging, errors);
        return errors;
    }

    private static HashSet<string> BuildExpectedFiles(PublishingContext context)
    {
        var expected = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            DeployPackageLayout.ManifestFile,
            DeployPackageLayout.HealthFile,
            DeployPackageLayout.MetadataFile,
            DeployPackageLayout.ConfigTemplateFile,
            DeployPackageLayout.ComposeFile,
            DeployPackageLayout.EnvTemplateFile,
            DeployPackageLayout.ServiceImageTar
        };

        if (context.DeploymentContracts is not null)
        {
            foreach (var contractFile in DeployPackageLayout.ContractFiles)
            {
                expected.Add(contractFile);
            }

            expected.Add(DeployPackageLayout.RuntimeCapabilitiesFile);

            // ServiceDeploymentBundleIO writes these two additional files only for the identity
            // service (IdentityDeploymentContractBuilder.IsIdentityService) — the generic
            // ContractFiles list above intentionally excludes them since every other service
            // never stages them. Without this, identity-service publishing always fails staging
            // integrity with two false "Unexpected staged artifact" errors.
            if (Aovias.Deployment.Contracts.Building.IdentityDeploymentContractBuilder.IsIdentityService(context.DeploymentContracts))
            {
                foreach (var identityContractFile in Aovias.Deployment.Contracts.DeploymentPackageLayout.IdentityContractFiles)
                {
                    expected.Add(identityContractFile);
                }
            }
        }

        return expected;
    }

    private static bool IsAllowedOrphan(string fileName)
        => string.Equals(fileName, DeployPackageLayout.ChecksumsFile, StringComparison.OrdinalIgnoreCase)
           || string.Equals(fileName, DeployPackageLayout.PublishingBuildManifestFile, StringComparison.OrdinalIgnoreCase)
           || string.Equals(fileName, DeployPackageLayout.DeploymentReadinessCertificateFile, StringComparison.OrdinalIgnoreCase)
           || string.Equals(fileName, DeployPackageLayout.PublishingReportFile, StringComparison.OrdinalIgnoreCase);

    private static void ValidateArtifactFreshness(string staging, List<string> errors)
    {
        var newestInputUtc = Directory.EnumerateFiles(staging)
            .Where(path => !IsAllowedOrphan(Path.GetFileName(path)!))
            .Select(path => new FileInfo(path).LastWriteTimeUtc)
            .DefaultIfEmpty(DateTime.UtcNow)
            .Max();

        foreach (var orphanName in new[] { DeployPackageLayout.ChecksumsFile, DeployPackageLayout.PublishingBuildManifestFile })
        {
            var path = Path.Combine(staging, orphanName);
            if (File.Exists(path) && new FileInfo(path).LastWriteTimeUtc < newestInputUtc.AddSeconds(-1))
            {
                errors.Add($"Stale staged artifact detected: {orphanName}");
            }
        }
    }
}
