namespace AIOrchestratorService.Application.Services;

/// <summary>
/// Audits AI-generated draft language to ensure it doesn't contain unsafe claims.
/// </summary>
public interface IDraftLanguageAuditor
{
    /// <summary>
    /// Audits the draft content for unsafe language.
    /// </summary>
    /// <param name="content">The draft content to audit.</param>
    /// <returns>Audit result indicating if unsafe language was found and what issues were detected.</returns>
    DraftLanguageAuditResult Audit(string content);
}

/// <summary>
/// Result of a language audit.
/// </summary>
public class DraftLanguageAuditResult
{
    /// <summary>
    /// Indicates whether unsafe language was detected.
    /// </summary>
    public bool HasUnsafeLanguage { get; init; }

    /// <summary>
    /// List of detected unsafe language issues.
    /// </summary>
    public List<string> Issues { get; init; } = new();

    /// <summary>
    /// Disclaimer header text to prepend if unsafe language is found.
    /// </summary>
    public string DisclaimerHeader { get; init; } = string.Empty;

    /// <summary>
    /// Creates a safe result (no unsafe language detected).
    /// </summary>
    public static DraftLanguageAuditResult Safe()
    {
        return new DraftLanguageAuditResult
        {
            HasUnsafeLanguage = false,
            Issues = new List<string>()
        };
    }

    /// <summary>
    /// Creates an unsafe result with detected issues.
    /// </summary>
    public static DraftLanguageAuditResult Unsafe(List<string> issues)
    {
        var disclaimer = BuildDisclaimerHeader(issues);
        return new DraftLanguageAuditResult
        {
            HasUnsafeLanguage = true,
            Issues = issues,
            DisclaimerHeader = disclaimer
        };
    }

    private static string BuildDisclaimerHeader(List<string> issues)
    {
        return $@"---
⚠️ DISCLAIMER: AI-GENERATED DRAFT
This draft contains language that may imply approval, certification, or regulatory authority.
This is an AI-assisted suggestion only and does not constitute official approval or certification.
Human review and validation are required before use.

Detected concerns:
{string.Join("\n", issues.Select(i => $"  • {i}"))}
---

";
    }
}
