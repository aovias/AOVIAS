namespace KnowledgeBase.Application.DTOs;

public enum KnowledgeByNodeCodeStatus
{
    NotFound,
    Ambiguous,
    Ok
}

public sealed record KnowledgeByNodeCodeResult(KnowledgeByNodeCodeStatus Status, IReadOnlyList<KnowledgeItemDto>? Items);

public sealed record NodeDetailsByCodeResult(KnowledgeByNodeCodeStatus Status, NodeDetailsDto? Details);

public sealed record FrameworkDto(Guid Id, string Code, string Name, string Version, string Description, bool IsActive);

public sealed record ComplianceNodeDto(
    Guid Id,
    Guid FrameworkId,
    Guid? ParentId,
    int Level,
    string Code,
    string Title,
    string Description,
    string NodeType,
    int Sequence);

public sealed record NodeDetailsDto(
    ComplianceNodeDto Node,
    RequirementDetailDto? Requirement,
    IReadOnlyList<KnowledgeItemDto> Knowledge,
    RequirementScoreDto? RequirementScore);

public sealed record RequirementDetailDto(
    Guid Id,
    Guid NodeId,
    string Intent,
    string MeasurableElements,
    string Classification,
    bool IsMandatory,
    string ApplicableDepartments,
    bool RequiresDocumentation);

public sealed record KnowledgeItemDto(
    Guid Id,
    Guid NodeId,
    string Explanation,
    string WhyItMatters,
    string HowToComply,
    string EvidenceRequired,
    string AuditChecklist,
    string RiskLevel,
    string ImpactLevel,
    string Tags,
    int Version,
    bool IsActive);

public sealed record RequirementScoreDto(Guid Id, Guid NodeId, decimal MaxScore, decimal Weight, Guid ClassificationId);

public sealed record AiAskRequestDto(Guid UserId, Guid FrameworkId, Guid? NodeId, string Query);

public sealed record AiAskResponseDto(Guid InteractionId, string Response, string ModelUsed, int TokensUsed);

public sealed record CreateDecisionRequestDto(
    Guid UserId,
    Guid FrameworkId,
    Guid NodeId,
    Guid AIInteractionId,
    string ProblemStatement,
    string SuggestedAction,
    string FinalDecision,
    string Outcome,
    decimal ConfidenceScore);

public sealed record DecisionLogDto(
    Guid Id,
    Guid UserId,
    Guid FrameworkId,
    Guid NodeId,
    Guid AIInteractionId,
    string ProblemStatement,
    string SuggestedAction,
    string FinalDecision,
    string Outcome,
    decimal ConfidenceScore,
    DateTime CreatedAt);

public sealed record CreateAssessmentRequestDto(Guid FacilityId, Guid FrameworkId);

public sealed record AssessmentDto(Guid Id, Guid FacilityId, Guid FrameworkId, string Status, DateTime CreatedAt);

public sealed record UpdateAssessmentItemRequestDto(
    int Score,
    string ComplianceStatus,
    string Remarks,
    bool HasActionPlan,
    bool DocumentationAdequate);

public sealed record AssessmentSummaryResponseDto(
    decimal OverallPercent,
    bool MeetsOverallPercent,
    bool CoreRulePassed,
    IReadOnlyList<Guid> CoreFailureNodeIds,
    IReadOnlyDictionary<Guid, decimal> ChapterAverages,
    IReadOnlyDictionary<Guid, decimal> StandardAverages,
    bool ChapterAveragesPassed,
    bool StandardAveragesPassed,
    bool CommitmentLevelPassed,
    bool TooManyLowScoresInStandard,
    IReadOnlyList<Guid> StandardsWithMultipleLowScores,
    IReadOnlyList<Guid> ActionPlanViolationNodeIds);

public sealed record WizardAssessmentAnswerDto(string QuestionId, string Question, string Answer);

public sealed record SubmitWizardAssessmentRequestDto(
    Guid FacilityId,
    Guid FrameworkId,
    IReadOnlyList<WizardAssessmentAnswerDto> Answers);

public sealed record SubmitWizardAssessmentResponseDto(
    Guid AssessmentId,
    AssessmentSummaryResponseDto Summary);
