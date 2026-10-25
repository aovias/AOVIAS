using AIOrchestratorService.Application.AI.Classification;

namespace AIOrchestratorService.Application.AI.Models;

public enum AIUseCase
{
    ExplainCompliance = 1,
    QueryCompliance = 2,
    SuggestMapping = 3,
    ValidateEvidence = 4,
    TaskRecommendation = 5
}

public enum AIValidationOutcome
{
    Accept = 1,
    Correct = 2,
    Reject = 3
}

public enum AIActionRiskClass
{
    Low = 1,
    High = 2
}

public sealed class AIRequest
{
    public string Prompt { get; set; } = string.Empty;
    public AIUseCase UseCase { get; set; }
    public Guid HospitalId { get; set; }
    public Guid? FrameworkId { get; set; }
    public string? Department { get; set; }
    public Guid? ObjectiveElementId { get; set; }
    public Guid? TaskId { get; set; }
    public Guid? EvidenceId { get; set; }
    public string ContextVersionId { get; set; } = string.Empty;
    public string PromptTemplateId { get; set; } = string.Empty;
    public string PromptTemplateVersion { get; set; } = string.Empty;
    public string? IdempotencyToken { get; set; }

    /// <summary>Page/module context (Dcc, ComplianceMaturity, AnalyticsHub, Journey).</summary>
    public string? PageContext { get; set; }

    public Guid? OrganizationId { get; set; }

    public string? EntityType { get; set; }

    public string? EntityId { get; set; }

    public string? ConversationSummary { get; set; }

    public List<string> RecentEntityRefs { get; set; } = new();

    /// <summary>Phase 9 — governed AI assistant profile executing this request.</summary>
    public Guid? AiProfileId { get; set; }

    /// <summary>Authenticated user for conversation memory scoping (optional; resolved from claims when omitted).</summary>
    public Guid? UserId { get; set; }
}

public sealed class EvidenceReviewRequest
{
    public Guid HospitalId { get; set; }
    public Guid? FrameworkId { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? ObjectiveElementId { get; set; }
    public Guid? EvidenceId { get; set; }
    public string? EvidenceTitle { get; set; }
    public string? EvidenceContentSummary { get; set; }
    public DateTime? EvidenceLastModifiedUtc { get; set; }
    public string? DocumentVersion { get; set; }
    public List<string> LinkedEvidenceIds { get; set; } = new();
}

public sealed class EvidenceReviewScoreCard
{
    public decimal CompletenessScore { get; set; }
    public decimal FreshnessScore { get; set; }
    public decimal DuplicateRiskScore { get; set; }
    public decimal VersionConsistencyScore { get; set; }
    public decimal OeCoverageScore { get; set; }
    public decimal OverallScore { get; set; }
}

public sealed class EvidenceReviewResult
{
    public EvidenceReviewScoreCard Scores { get; set; } = new();
    public string Recommendation { get; set; } = string.Empty;
    public List<AIReasoningStep> Reasoning { get; set; } = new();
    public List<AISourceReference> Citations { get; set; } = new();
    public bool HumanApprovalRequired { get; set; } = true;
    public string TraceId { get; set; } = string.Empty;
}

public sealed class CapaSuggestRequest
{
    public Guid HospitalId { get; set; }
    public Guid? FrameworkId { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? DeficiencyId { get; set; }
    public Guid? ObjectiveElementId { get; set; }
    public string? DeficiencySummary { get; set; }
    public string? RootCauseHint { get; set; }
    public bool UseLlmAssist { get; set; }
}

public sealed class CapaSuggestionItem
{
    public string Kind { get; set; } = "Corrective"; // Corrective | Preventive
    public string Title { get; set; } = string.Empty;
    public string Proposal { get; set; } = string.Empty;
    public string ExpectedOutcome { get; set; } = string.Empty;
    public decimal ConfidenceScore { get; set; }
    public List<AISourceReference> Citations { get; set; } = new();
}

public sealed class CapaSuggestResult
{
    public List<CapaSuggestionItem> Suggestions { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
    public bool HumanApprovalRequired { get; set; } = true;
    public string TraceId { get; set; } = string.Empty;
    public GroundedAnswer? Grounded { get; set; }
}

public sealed class AISuggestedMapping
{
    public Guid? ObjectiveElementId { get; set; }
    public Guid? TaskId { get; set; }
    public string? MappingReason { get; set; }
}

public sealed class AISuggestedAction
{
    public string ToolName { get; set; } = string.Empty;
    public Dictionary<string, string> Parameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public AIActionRiskClass RiskClass { get; set; }
}

public sealed class AIReasoningStep
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public sealed class AISourceReference
{
    public string SourceType { get; set; } = string.Empty;
    public string SourceId { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public decimal RelevanceScore { get; set; }

    /// <summary>Phase 5 — version-aware citation lineage.</summary>
    public string? DocumentId { get; set; }
    public string? Version { get; set; }
    public string? Excerpt { get; set; }
}

/// <summary>
/// Canonical grounded AI answer contract for the AOVIAS Intelligence Layer.
/// Every user-facing AI capability should project into this shape (or AIResponse which embeds it).
/// </summary>
public sealed class GroundedAnswer
{
    public string Answer { get; set; } = string.Empty;
    public List<AISourceReference> Citations { get; set; } = new();
    public List<AIReasoningStep> Reasoning { get; set; } = new();
    public decimal ConfidenceScore { get; set; }
    public string ModelVersion { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
    public AssistantAnswerMode AnswerMode { get; set; } = AssistantAnswerMode.GeneralKnowledge;
    public bool HumanApprovalRequired { get; set; }
    public AIRetrievalContext? Retrieval { get; set; }
    public ComplianceResponseModel? Presentation { get; set; }
}

public sealed class KnowledgeQuery
{
    public string Query { get; set; } = string.Empty;
    public Guid HospitalId { get; set; }
    public Guid? FrameworkId { get; set; }
    /// <summary>keyword | semantic | hybrid</summary>
    public string Mode { get; set; } = "hybrid";
    public int TopK { get; set; } = 10;
}

public sealed class KnowledgeSearchResult
{
    public string Query { get; set; } = string.Empty;
    public string Mode { get; set; } = "hybrid";
    public List<AISourceReference> Citations { get; set; } = new();
    public int Total { get; set; }
}

public sealed class AIResponse
{
    public AISuggestedAction? SuggestedAction { get; set; }
    public AISuggestedMapping? SuggestedMapping { get; set; }
    public string ComplianceHint { get; set; } = string.Empty;
    public decimal ConfidenceScore { get; set; }
    public decimal CompositeConfidenceScore { get; set; }
    public List<AIReasoningStep> Reasoning { get; set; } = new();
    public List<AISourceReference> SourceReferences { get; set; } = new();
    public AIValidationOutcome ValidationOutcome { get; set; }
    public string ContextVersionId { get; set; } = string.Empty;
    public string PromptTemplateId { get; set; } = string.Empty;
    public string PromptTemplateVersion { get; set; } = string.Empty;
    public bool HumanApprovalRequired { get; set; }
    public bool WasFallback { get; set; }
    public AssistantAnswerMode AnswerMode { get; set; } = AssistantAnswerMode.GeneralKnowledge;

    /// <summary>
    /// Executive Compliance Advisor presentation model. Additive and nullable so existing
    /// clients that read <see cref="ComplianceHint"/> and the flat fields keep working.
    /// </summary>
    public ComplianceResponseModel? Presentation { get; set; }

    /// <summary>Phase 5 — explicit grounded answer projection (citations + confidence + model).</summary>
    public GroundedAnswer? Grounded { get; set; }

    public string? ModelVersion { get; set; }
    public string? TraceId { get; set; }
}

/// <summary>
/// Business-friendly severity used across findings and risks. Drives badge colors in the UI
/// (Critical = red, High = orange, Medium = yellow, Low = green).
/// </summary>
public enum ComplianceSeverity
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

/// <summary>
/// Shape of the compliance question so the UI can adapt section headers
/// (e.g. "Top Compliance Gaps" vs "Missing Evidence").
/// </summary>
public enum ComplianceInsightType
{
    General = 0,
    ComplianceScore = 1,
    Risk = 2,
    Evidence = 3,
    Standard = 4
}

public sealed class ComplianceFinding
{
    public string Title { get; set; } = string.Empty;
    public ComplianceSeverity Severity { get; set; } = ComplianceSeverity.Medium;
    public string Impact { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
}

public sealed class ComplianceRisk
{
    public string Title { get; set; } = string.Empty;
    public ComplianceSeverity Severity { get; set; } = ComplianceSeverity.Medium;
    public List<string> AffectedControls { get; set; } = new();
}

public sealed class RecommendedAction
{
    public int Priority { get; set; }
    public string Action { get; set; } = string.Empty;
    public string ExpectedOutcome { get; set; } = string.Empty;
}

public sealed class SupportingEvidenceItem
{
    public string Label { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
}

/// <summary>
/// Internal/technical details deliberately hidden from the default executive view and only
/// surfaced inside the collapsible "Supporting Evidence" panel for advanced users.
/// </summary>
public sealed class TechnicalMetadata
{
    public int Confidence { get; set; }
    public string AnswerMode { get; set; } = string.Empty;
    public string ContextVersionId { get; set; } = string.Empty;
    public List<AISourceReference> RawSources { get; set; } = new();
}

/// <summary>
/// Executive Compliance Advisor presentation contract. Every user-facing string is already
/// sanitized of AI/retrieval terminology by the presentation layer.
/// </summary>
public sealed class ComplianceResponseModel
{
    public ComplianceInsightType InsightType { get; set; } = ComplianceInsightType.General;
    public string ExecutiveSummary { get; set; } = string.Empty;
    public List<ComplianceFinding> Findings { get; set; } = new();
    public List<ComplianceRisk> Risks { get; set; } = new();
    public List<RecommendedAction> Recommendations { get; set; } = new();
    public string ExpectedOutcome { get; set; } = string.Empty;
    public List<SupportingEvidenceItem> SupportingEvidence { get; set; } = new();
    public TechnicalMetadata Technical { get; set; } = new();

    public int FindingCount => Findings.Count;
    public int RiskCount => Risks.Count;
    public int ActionCount => Recommendations.Count;
    public int EvidenceCount => SupportingEvidence.Count;
}

public sealed class AIRetrievalContext
{
    public string ContextVersionId { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public List<AISourceReference> Sources { get; set; } = new();
    public Dictionary<string, string> Facts { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public decimal RelevanceScore { get; set; }
}

public sealed class AIToolPreviewResult
{
    public bool Allowed { get; set; }
    public string Reason { get; set; } = string.Empty;
    public AISuggestedAction? Action { get; set; }
    public Dictionary<string, string> ImpactSummary { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
