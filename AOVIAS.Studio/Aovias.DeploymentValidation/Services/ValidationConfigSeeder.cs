using Aovias.Deployment.Contracts;

namespace Aovias.DeploymentValidation.Services;

/// <summary>
/// Seeds a minimal validation config tree without copying storage or publishing artifacts.
/// </summary>
public static class ValidationConfigSeeder
{
    private static readonly string[] IncludedSubdirectories = ["profiles", "secrets", "certificates"];

    private static readonly HashSet<string> ExcludedDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "storage",
        "node_modules",
        "bin",
        "obj",
        ".git"
    };

    public static void Seed(string validationConfigRoot, string configRoot, string validationProfile, CertificateContract? certificateContract = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(validationConfigRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(configRoot);

        if (Directory.Exists(configRoot))
        {
            Directory.Delete(configRoot, recursive: true);
        }

        Directory.CreateDirectory(configRoot);

        if (!Directory.Exists(validationConfigRoot))
        {
            IdentitySigningCertificateProvisioner.EnsureProvisioned(configRoot, certificateContract);
            return;
        }

        foreach (var subdirectory in IncludedSubdirectories)
        {
            var source = Path.Combine(validationConfigRoot, subdirectory);
            if (!Directory.Exists(source))
            {
                continue;
            }

            CopyDirectoryFiltered(source, Path.Combine(configRoot, subdirectory));
        }

        foreach (var file in Directory.EnumerateFiles(validationConfigRoot, "*.json"))
        {
            File.Copy(file, Path.Combine(configRoot, Path.GetFileName(file)), overwrite: true);
        }

        TemporaryDeploymentWorkspace.PatchValidationLoggingSettings(configRoot, validationProfile);
        TemporaryDeploymentWorkspace.PatchValidationRuntimePaths(configRoot, validationProfile);
        IdentitySigningCertificateProvisioner.EnsureProvisioned(configRoot, certificateContract);
    }

    private static void CopyDirectoryFiltered(string source, string destination)
    {
        Directory.CreateDirectory(destination);

        foreach (var directory in Directory.EnumerateDirectories(source))
        {
            var name = Path.GetFileName(directory);
            if (ExcludedDirectoryNames.Contains(name))
            {
                continue;
            }

            CopyDirectoryFiltered(directory, Path.Combine(destination, name));
        }

        foreach (var file in Directory.EnumerateFiles(source))
        {
            var target = Path.Combine(destination, Path.GetFileName(file));
            File.Copy(file, target, overwrite: true);
        }
    }
}
