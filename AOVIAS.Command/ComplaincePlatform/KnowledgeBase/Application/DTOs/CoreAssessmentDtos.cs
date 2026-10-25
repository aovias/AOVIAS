namespace KnowledgeBase.Application.DTOs;

/// <summary>Single question in a Core maturity assessment.</summary>
public record CoreAssessmentQuestionDto(
    Guid   WizardQuestionId,
    Guid   ObjectiveElementId,
    string QuestionTitle,
    string QuestionText,
    string HelpText,
    string AssessmentGuidance,
    string ExpectedEvidence,
    string Category,
    decimal Weight,
    bool   IsMandatory,
    bool   IsCritical,
    int    DisplayPriority);

/// <summary>Full Core assessment payload returned to the caller.</summary>
public record CoreAssessmentDto(
    Guid   FrameworkId,
    string FrameworkName,
    string MaturityLevel,
    IReadOnlyList<CoreAssessmentQuestionDto> Questions);

// ── Submission DTOs ──────────────────────────────────────────────────────────

/// <summary>A single answered question submitted from the Core wizard step.</summary>
public record CoreWizardAnswerDto(
    Guid   WizardQuestionId,
    Guid   ObjectiveElementId,
    /// <summary>compliant | partially_compliant | non_compliant | not_applicable</summary>
    string Answer,
    decimal Weight,
    bool   IsMandatory,
    bool   IsCritical);

/// <summary>Full submission body for POST assessments/from-core-wizard.</summary>
public sealed class SubmitCoreWizardRequestDto
{
    public Guid   FrameworkId  { get; init; }
    public Guid   FacilityId   { get; init; }
    public string AssessedBy   { get; init; } = string.Empty;
    public IReadOnlyList<CoreWizardAnswerDto> Answers { get; init; } = [];
}

/// <summary>Gap category summary item in the response.</summary>
public record GapCategoryDto(
    string Category,
    int    NonCompliantCount,
    int    PartiallyCompliantCount,
    string[] TopGuidance);

/// <summary>Core gap summary returned as part of the submission response.</summary>
public record CoreGapSummaryDto(
    decimal CoreReadinessScore,
    string  Status,
    int     CriticalFailureCount,
    IReadOnlyList<GapCategoryDto> HighRiskAreas,
    IReadOnlyList<string> RecommendedNextActions,
    bool    CommitmentAssessmentEligible,
    string? BlockingReason);

/// <summary>Response body for POST assessments/from-core-wizard.</summary>
public sealed class SubmitCoreWizardResponseDto
{
    public Guid    AssessmentId           { get; init; }
    public decimal CoreReadinessScore     { get; init; }
    public string  Status                 { get; init; } = string.Empty;  // Pass | Fail
    public int     CriticalFailureCount   { get; init; }
    public bool    CanUnlockCommitment    { get; init; }
    public IReadOnlyList<string> HighRiskAreas { get; init; } = [];
    public CoreGapSummaryDto? GapSummary  { get; init; }
}
