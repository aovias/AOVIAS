using System.Text.RegularExpressions;

namespace ComplianceServiceManager.Services.Presentation;

/// <summary>
/// Minimal semantic-version parsing shared by the Publishing version-conflict UI and the
/// operator error cards. The Release Library itself treats <c>Version</c> as an opaque string
/// (see <c>ReleaseArtifactRecord.Version</c>) — this helper only interprets it for suggesting the
/// operator's next version; it is never used to validate or gate what the backend accepts.
/// </summary>
public static class SemanticVersionHelper
{
    private static readonly Regex CoreVersion = new(@"^(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)$", RegexOptions.Compiled);

    public static bool IsValid(string? version) => !string.IsNullOrWhiteSpace(version) && CoreVersion.IsMatch(version.Trim());

    public enum VersionPart { Major, Minor, Patch }

    /// <summary>Bumps the requested segment, zeroing the segments below it. Non-semver input gets a ".1" suffix.</summary>
    public static string Bump(string version, VersionPart part)
    {
        var match = CoreVersion.Match(version ?? string.Empty);
        if (!match.Success)
        {
            return string.IsNullOrWhiteSpace(version) ? "1.0.0" : version + ".1";
        }

        var major = int.Parse(match.Groups["major"].Value);
        var minor = int.Parse(match.Groups["minor"].Value);
        var patch = int.Parse(match.Groups["patch"].Value);

        return part switch
        {
            VersionPart.Major => $"{major + 1}.0.0",
            VersionPart.Minor => $"{major}.{minor + 1}.0",
            _ => $"{major}.{minor}.{patch + 1}"
        };
    }

    /// <summary>Highest version in the set by semantic ordering when possible, else lexical fallback.</summary>
    public static string? FindLatest(IEnumerable<string> versions)
    {
        var list = versions.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
        if (list.Count == 0)
        {
            return null;
        }

        return list
            .OrderByDescending(v => CoreVersion.IsMatch(v))
            .ThenByDescending(v => ParseSortKey(v))
            .First();
    }

    private static (int, int, int) ParseSortKey(string version)
    {
        var match = CoreVersion.Match(version);
        return match.Success
            ? (int.Parse(match.Groups["major"].Value), int.Parse(match.Groups["minor"].Value), int.Parse(match.Groups["patch"].Value))
            : (0, 0, 0);
    }
}
