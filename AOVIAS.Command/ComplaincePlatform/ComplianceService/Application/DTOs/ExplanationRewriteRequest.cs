namespace ComplianceService.Application.DTOs;

/// <summary>
/// Request object for rewriting an existing explanation text.
/// This represents a REWRITE request, not a generation request.
/// 
/// Constraints:
/// - Do not add facts: The rewrite must not introduce new factual information
/// - Do not remove facts: The rewrite must preserve all factual information from the original
/// - Do not infer conclusions: The rewrite must not add conclusions or inferences not present in the original
/// </summary>
public class ExplanationRewriteRequest
{
    /// <summary>
    /// The original explanation text to be rewritten.
    /// </summary>
    public string OriginalExplanationText { get; set; } = string.Empty;

    /// <summary>
    /// The target audience type for the rewritten explanation.
    /// </summary>
    public ExplanationAudienceType AudienceType { get; set; }

    /// <summary>
    /// The desired tone for the rewritten explanation.
    /// </summary>
    public ExplanationTone Tone { get; set; }
}

/// <summary>
/// Target audience type for explanation rewrite.
/// </summary>
public enum ExplanationAudienceType
{
    /// <summary>
    /// Administrative audience - typically needs high-level overview.
    /// </summary>
    Admin,

    /// <summary>
    /// Compliance officer audience - typically needs detailed technical information.
    /// </summary>
    ComplianceOfficer,

    /// <summary>
    /// Auditor audience - typically needs structured, evidence-focused information.
    /// </summary>
    Auditor
}

/// <summary>
/// Desired tone for explanation rewrite.
/// </summary>
public enum ExplanationTone
{
    /// <summary>
    /// Neutral tone - objective and factual without emotional coloring.
    /// </summary>
    Neutral,

    /// <summary>
    /// Supportive tone - helpful and assistive, guiding the reader.
    /// </summary>
    Supportive,

    /// <summary>
    /// Formal tone - professional and structured, suitable for official documentation.
    /// </summary>
    Formal
}
