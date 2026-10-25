using Aovias.Publishing.Contracts;

namespace Aovias.DeploymentValidation.Rules;

/// <summary>
/// Scans deployment package artifacts for environment-neutral violations.
/// </summary>
public static class PackageArtifactScanner
{
    private static readonly string[] TextExtensions =
    [
        ".json", ".yml", ".yaml", ".env", ".template", ".sh", ".ps1", ".cmd", ".txt", ".md"
    ];

    public static IReadOnlyList<string> ScanDirectory(
        string directory,
        bool includeComposeHealthcheckAllowance = true,
        bool allowWindowsHostPaths = false)
    {
        var errors = new List<string>();
        if (!Directory.Exists(directory))
        {
            return errors;
        }

        foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
        {
            if (file.EndsWith(".tar", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!IsTextArtifact(file))
            {
                continue;
            }

            var content = File.ReadAllText(file);
            var artifactName = Path.GetFileName(file);
            var profile = string.Equals(artifactName, DeployPackageLayout.ComposeFile, StringComparison.OrdinalIgnoreCase)
                && includeComposeHealthcheckAllowance
                ? EnvironmentNeutralScanProfile.ComposeFile
                : EnvironmentNeutralScanProfile.PackageArtifact;

            errors.AddRange(EnvironmentNeutralRules.FindViolations(content, artifactName, profile, allowWindowsHostPaths));
        }

        return errors;
    }

    public static IReadOnlyList<string> ScanText(
        string content,
        string artifactName,
        EnvironmentNeutralScanProfile profile = EnvironmentNeutralScanProfile.PackageArtifact,
        bool allowWindowsHostPaths = false)
        => EnvironmentNeutralRules.FindViolations(content, artifactName, profile, allowWindowsHostPaths);

    private static bool IsTextArtifact(string path)
    {
        var extension = Path.GetExtension(path);
        return TextExtensions.Any(candidate => extension.Equals(candidate, StringComparison.OrdinalIgnoreCase))
               || Path.GetFileName(path).StartsWith(".env", StringComparison.OrdinalIgnoreCase);
    }
}
