using System.Text.RegularExpressions;

namespace ComplianceServiceManager.Services.Presentation;

/// <summary>
/// A parsed "duplicate artifact ID, different fingerprint" failure from the publishing pipeline.
/// Shared by the pipeline-stage message (<c>PublishingStageExecutor.RunReleaseLibraryAsync</c>,
/// wording "already contains artifact") and the Release Library registration exception
/// (<c>ReleaseLibraryService.RegisterPublishedAsync</c>, wording "duplicate artifact ID") — the two
/// backend layers that enforce ADR-0011/D10 fingerprint immutability use different message text for
/// what is, to an operator, the same event.
/// </summary>
public sealed record ReleaseConflictInfo(string ArtifactId, string ExistingFingerprint, string NewFingerprint, string Version);

public static class ReleaseConflictParser
{
    // Stage A: PublishingStageExecutor.RunReleaseLibraryAsync (pre-flight, before registration).
    private static readonly Regex StageWording = new(
        @"already contains artifact '(?<artifact>[^']+)' with a different publishing fingerprint \(existing='(?<existing>[^']+)', new='(?<new>[^']+)'\).*reusing '(?<version>[^']+)'",
        RegexOptions.Compiled | RegexOptions.Singleline);

    // Stage B: ReleaseLibraryService.RegisterPublishedAsync (authoritative registration exception).
    private static readonly Regex RegistrationWording = new(
        @"duplicate artifact ID '(?<artifact>[^']+)' with a different publishing fingerprint\. Existing='(?<existing>[^']+)', new='(?<new>[^']+)'",
        RegexOptions.Compiled | RegexOptions.Singleline);

    public static bool TryParse(string? message, out ReleaseConflictInfo info)
    {
        var text = message ?? string.Empty;

        var match = StageWording.Match(text);
        if (match.Success)
        {
            info = new ReleaseConflictInfo(
                match.Groups["artifact"].Value,
                match.Groups["existing"].Value,
                match.Groups["new"].Value,
                match.Groups["version"].Value);
            return true;
        }

        match = RegistrationWording.Match(text);
        if (match.Success)
        {
            var artifact = match.Groups["artifact"].Value;
            var version = artifact.Contains('-') ? artifact[(artifact.LastIndexOf('-') + 1)..] : artifact;
            info = new ReleaseConflictInfo(artifact, match.Groups["existing"].Value, match.Groups["new"].Value, version);
            return true;
        }

        info = null!;
        return false;
    }
}
