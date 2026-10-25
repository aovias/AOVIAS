using System.Text.RegularExpressions;
using ComplianceServiceManager.Models;

namespace ComplianceServiceManager.Services.Presentation;

/// <summary>
/// Presentation-layer translator from raw engine/tool diagnostics to enterprise operator
/// errors. Pure string interpretation — it never alters what happened, only how it is shown.
/// Unrecognized messages fall back to a generic card that still keeps the raw text in the
/// technical-details panel rather than dumping it into the primary UI.
/// </summary>
public static class OperatorErrorInterpreter
{
    private static readonly Regex MissingPathRegex = new(
        @"Could not find a part of the path '(?<path>[^']+)'",
        RegexOptions.Compiled);

    public static OperatorErrorItem Interpret(string serviceId, string rawMessage)
    {
        var raw = rawMessage?.Trim() ?? string.Empty;
        var display = ToDisplayName(serviceId);

        if (ReleaseConflictParser.TryParse(raw, out var conflict))
        {
            var version = conflict.Version;
            return new OperatorErrorItem
            {
                Title = "Release already exists",
                Category = "Release Conflict",
                Subject = display,
                SubjectDetail = $"Version {version}",
                Problem = $"A different build already exists for version {version}. " +
                          "The Release Library never overwrites a published version with different content.",
                RecommendedActions =
                [
                    $"Publish as a new version (for example {SuggestNextVersion(version)})",
                    "Open Release Library and review the existing release",
                    "Compare the fingerprint difference in the technical details below"
                ],
                Details = raw
            };
        }

        var missingPath = MissingPathRegex.Match(raw);
        if (missingPath.Success)
        {
            return new OperatorErrorItem
            {
                Title = "Working folder unavailable",
                Category = "Environment",
                Subject = display,
                Problem = "A folder the publish run depended on no longer exists. " +
                          "This usually means a previous run's workspace was cleaned up mid-flight.",
                RecommendedActions =
                [
                    "Run the publish again — workspaces are recreated on each run",
                    "If it recurs, restart AOVIAS Studio to reset the publishing session"
                ],
                Details = raw
            };
        }

        if (raw.Contains("docker compose", StringComparison.OrdinalIgnoreCase)
            || raw.Contains("Docker Compose", StringComparison.Ordinal))
        {
            return new OperatorErrorItem
            {
                Title = "Container configuration failed validation",
                Category = "Infrastructure",
                Subject = display,
                Problem = "The generated container configuration did not pass Docker's validation for this service.",
                RecommendedActions =
                [
                    "Review the service's infrastructure specification",
                    "Verify Docker Desktop is running and healthy",
                    "Inspect the full Docker output in the technical details below"
                ],
                Details = raw
            };
        }

        if (raw.Contains("NETSDK", StringComparison.Ordinal)
            || Regex.IsMatch(raw, @"\bCS\d{4}\b")
            || raw.Contains("MSB", StringComparison.Ordinal))
        {
            return new OperatorErrorItem
            {
                Title = "Build failed",
                Category = "Build",
                Subject = display,
                Problem = "The service did not compile or publish cleanly.",
                RecommendedActions =
                [
                    "Open the compiler output in the technical details below",
                    "Build the service locally to reproduce the failure"
                ],
                Details = raw
            };
        }

        if (raw.Contains("npm", StringComparison.OrdinalIgnoreCase)
            || raw.Contains("node", StringComparison.OrdinalIgnoreCase) && raw.Contains("--version", StringComparison.Ordinal))
        {
            return new OperatorErrorItem
            {
                Title = "Frontend tooling failed",
                Category = "Frontend Tooling",
                Subject = display,
                Problem = "The Node/npm toolchain reported an error while preparing the frontend.",
                RecommendedActions =
                [
                    "Close editors or antivirus scans holding files in node_modules",
                    "Delete node_modules and reinstall dependencies",
                    "Verify Node.js is installed and on PATH"
                ],
                Details = raw
            };
        }

        return new OperatorErrorItem
        {
            Title = "Publishing failed",
            Category = "Publishing",
            Subject = display,
            Problem = string.IsNullOrWhiteSpace(raw)
                ? "The step failed without producing a diagnostic message."
                : Truncate(FirstLine(raw), 220),
            RecommendedActions =
            [
                "Open the technical details below for the full diagnostic",
                "Check Operations → Alerts for related warnings"
            ],
            Details = raw
        };
    }

    private static string ToDisplayName(string serviceId)
    {
        if (string.IsNullOrWhiteSpace(serviceId))
        {
            return string.Empty;
        }

        var words = serviceId.Replace('-', ' ').Replace('_', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', words.Select(word => char.ToUpperInvariant(word[0]) + word[1..]));
    }

    private static string SuggestNextVersion(string version)
        => SemanticVersionHelper.Bump(version, SemanticVersionHelper.VersionPart.Patch);

    private static string FirstLine(string text)
    {
        var index = text.IndexOfAny(['\r', '\n']);
        return index < 0 ? text : text[..index];
    }

    private static string Truncate(string text, int max)
        => text.Length <= max ? text : text[..max] + "…";
}
