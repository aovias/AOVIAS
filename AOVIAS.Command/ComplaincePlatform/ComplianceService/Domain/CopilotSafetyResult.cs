namespace ComplianceService.Domain;

/// <summary>
/// Result of safety validation for Copilot responses.
/// Indicates if unsafe language was detected and what warnings should be shown.
/// </summary>
public class CopilotSafetyResult
{
    /// <summary>
    /// Indicates if the response passed safety validation.
    /// </summary>
    public bool IsSafe { get; set; } = true;

    /// <summary>
    /// List of safety violations detected.
    /// </summary>
    public List<SafetyViolation> Violations { get; set; } = new();

    /// <summary>
    /// Warning banner text to display if unsafe language is detected.
    /// </summary>
    public string? WarningBanner { get; set; }

    /// <summary>
    /// Indicates if a warning banner should be displayed.
    /// </summary>
    public bool RequiresWarningBanner => !IsSafe && Violations.Count > 0;
}

/// <summary>
/// Represents a safety violation detected in the response.
/// </summary>
public class SafetyViolation
{
    /// <summary>
    /// Type of safety violation.
    /// </summary>
    public SafetyViolationType ViolationType { get; set; }

    /// <summary>
    /// Description of the violation.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The text that triggered the violation (sample).
    /// </summary>
    public string? TriggeringText { get; set; }

    /// <summary>
    /// Severity of the violation.
    /// </summary>
    public SafetyViolationSeverity Severity { get; set; }
}

/// <summary>
/// Type of safety violation.
/// </summary>
public enum SafetyViolationType
{
    /// <summary>
    /// Language that implies approval or compliance certification.
    /// </summary>
    ImpliesApprovalOrCompliance,

    /// <summary>
    /// Language that predicts audit outcomes.
    /// </summary>
    PredictsAuditOutcome,

    /// <summary>
    /// Language that claims regulator authority.
    /// </summary>
    ClaimsRegulatorAuthority,

    /// <summary>
    /// Language that is not assistive (too definitive, authoritative, etc.).
    /// </summary>
    NonAssistiveLanguage
}

/// <summary>
/// Severity of a safety violation.
/// </summary>
public enum SafetyViolationSeverity
{
    /// <summary>
    /// High severity - clear violation of safety rules.
    /// </summary>
    High,

    /// <summary>
    /// Medium severity - potential violation that should be flagged.
    /// </summary>
    Medium,

    /// <summary>
    /// Low severity - minor concern that may need review.
    /// </summary>
    Low
}
