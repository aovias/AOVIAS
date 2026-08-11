using System.Text.RegularExpressions;

namespace Aovias.DeploymentValidation.Rules;

/// <summary>
/// Scan profile controlling which environment-neutrality rules apply.
/// </summary>
public enum EnvironmentNeutralScanProfile
{
    PackageArtifact,
    ComposeFile,
    BootstrapOutput
}

/// <summary>
/// Forbidden patterns that must not appear in deployment artifacts.
/// </summary>
public static class EnvironmentNeutralRules
{
    public const string DrvVersion = "1.0.0";

    public static readonly IReadOnlyList<string> ForbiddenTokens =
    [
        "V:/DevelopmentRepo",
        "V:\\DevelopmentRepo",
        "DevelopmentRepo\\V1.0",
        "DevelopmentRepo/V1.0",
        "aovias-local.dev",
        "\"ActiveProfile\": \"local\"",
        "\"ConfigRoot\": \"V:",
        "CN=localhost",
        "C:\\Users\\",
        "C:/Users/",
        "Profile=local",
        "ActiveProfile=local"
    ];

    public static readonly IReadOnlyList<string> HardcodedNetworkNames =
    [
        "aovias-runtime-net"
    ];

    public static readonly Regex WindowsAbsolutePathPattern = new(
        @"[A-Za-z]:\\",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static readonly Regex WindowsDrivePathPattern = new(
        @"(?<![a-zA-Z])([A-Za-z]:/)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static readonly Regex LocalhostPattern = new(
        @"\blocalhost\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static readonly Regex LoopbackPattern = new(
        @"\b127\.0\.0\.1\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static readonly Regex DeveloperUsernamePattern = new(
        @"[\\/]Users[\\/][^\\/""]+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>Personal folders that must never appear in deployment artifacts.</summary>
    public static readonly Regex PersonalFolderPattern = new(
        @"[\\/](Desktop|Downloads|Documents)(?=[\\/]|$|"")",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>UNC / machine-name network share references (for example <c>\\\\MACHINE\\share</c>).</summary>
    public static readonly Regex UncMachinePathPattern = new(
        @"\\\\[A-Za-z0-9._-]+\\",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static IReadOnlyList<string> FindViolations(
        string content,
        string artifactName,
        EnvironmentNeutralScanProfile profile = EnvironmentNeutralScanProfile.PackageArtifact,
        bool allowWindowsHostPaths = false)
    {
        var violations = new List<string>();

        foreach (var token in ForbiddenTokens)
        {
            if (content.Contains(token, StringComparison.OrdinalIgnoreCase))
            {
                violations.Add($"{artifactName} contains forbidden token '{token}'.");
            }
        }

        foreach (var network in HardcodedNetworkNames)
        {
            if (content.Contains(network, StringComparison.OrdinalIgnoreCase))
            {
                violations.Add($"{artifactName} contains hardcoded network name '{network}'.");
            }
        }

        // Windows drive paths are forbidden unless the active profile explicitly
        // uses a Windows host convention (for example the local profile). UNC
        // machine shares and personal folders are NEVER allowed.
        if (profile != EnvironmentNeutralScanProfile.ComposeFile && !allowWindowsHostPaths)
        {
            foreach (Match match in WindowsAbsolutePathPattern.Matches(content))
            {
                violations.Add($"{artifactName} contains Windows-only path '{match.Value}'.");
            }

            foreach (Match match in WindowsDrivePathPattern.Matches(content))
            {
                violations.Add($"{artifactName} contains Windows drive path '{match.Value}'.");
            }
        }

        foreach (Match match in UncMachinePathPattern.Matches(content))
        {
            violations.Add($"{artifactName} contains machine/UNC network path '{match.Value}'.");
        }

        foreach (Match match in PersonalFolderPattern.Matches(content))
        {
            violations.Add($"{artifactName} contains personal folder reference '{match.Value}'.");
        }

        foreach (Match match in DeveloperUsernamePattern.Matches(content))
        {
            violations.Add($"{artifactName} contains developer user path '{match.Value}'.");
        }

        if (profile != EnvironmentNeutralScanProfile.ComposeFile)
        {
            foreach (Match match in LocalhostPattern.Matches(content))
            {
                violations.Add($"{artifactName} contains forbidden host '{match.Value}'.");
            }

            foreach (Match match in LoopbackPattern.Matches(content))
            {
                violations.Add($"{artifactName} contains forbidden loopback address '{match.Value}'.");
            }
        }

        return violations;
    }

    public static IReadOnlyList<string> ScanPackageArtifacts(string content, string artifactName, bool allowWindowsHostPaths = false)
        => FindViolations(content, artifactName, EnvironmentNeutralScanProfile.PackageArtifact, allowWindowsHostPaths);

    public static IReadOnlyList<string> ScanComposeFile(string content, string artifactName)
        => FindViolations(content, artifactName, EnvironmentNeutralScanProfile.ComposeFile);

    public static IReadOnlyList<string> ScanBootstrapOutput(string content, string artifactName)
        => FindViolations(content, artifactName, EnvironmentNeutralScanProfile.BootstrapOutput);
}
