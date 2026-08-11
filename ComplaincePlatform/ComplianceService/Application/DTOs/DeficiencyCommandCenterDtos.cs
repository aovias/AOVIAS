using ComplianceService.Application.Contracts;

namespace ComplianceService.Application.DTOs;

/// <summary>Command center dashboard KPIs (Sprint 6 H5).</summary>
public class DeficiencyCommandCenterDashboardDto
{
    public int TotalDeficiencies { get; set; }
    public int CriticalDeficiencies { get; set; }
    public int MajorDeficiencies { get; set; }
    public int OpenDeficiencies { get; set; }
    public int ResolvedDeficiencies { get; set; }
    public int BlockingDeficiencies { get; set; }
    /// <summary>Advisory count — not compliance deficiencies (from CM predicted gaps).</summary>
    public int PredictedGaps { get; set; }
    /// <summary>Audit findings count — separate from compliance deficiencies.</summary>
    public int AuditFindings { get; set; }
    public int AuditOriginDeficiencies { get; set; }
    public int AssessmentOriginDeficiencies { get; set; }
    public int ManualOriginDeficiencies { get; set; }
    public int ModerateDeficiencies { get; set; }
    public int MinorDeficiencies { get; set; }

    // Sprint 8 lifecycle KPIs
    public int AwaitingAssignment { get; set; }
    public int InProgressDeficiencies { get; set; }
    public int AwaitingEvidence { get; set; }
    public int VerificationPending { get; set; }
    public int VerifiedDeficiencies { get; set; }
    public int HighRiskDeficiencies { get; set; }
    public int CriticalRiskDeficiencies { get; set; }

    // Sprint 10 SLA KPIs
    public int SlaOnTrack { get; set; }
    public int SlaWarning { get; set; }
    public int SlaAtRisk { get; set; }
    public int SlaBreached { get; set; }
    public int EscalatedDeficiencies { get; set; }
    public int CriticalEscalations { get; set; }

    // Sprint 11 / Phase 2 CAPA KPIs
    public int OpenCapas { get; set; }
    public int OverdueCapas { get; set; }
    public int EffectiveCapas { get; set; }
    public int IneffectiveCapas { get; set; }
    public int RecurringIssues { get; set; }
    public int PendingEffectivenessReviews { get; set; }
    public double AverageOpenCapaAgeDays { get; set; }
    public CapaAgingBucketsDto AgingBuckets { get; set; } = new();
    public IReadOnlyList<DeficiencyCountByLabelDto> DepartmentOverdueCapas { get; set; }
        = Array.Empty<DeficiencyCountByLabelDto>();

    public IReadOnlyList<DeficiencyCountByLabelDto> ByRootCauseCategory { get; set; } = Array.Empty<DeficiencyCountByLabelDto>();
    public IReadOnlyList<DeficiencyCountByLabelDto> ByRiskLevel { get; set; } = Array.Empty<DeficiencyCountByLabelDto>();
    public IReadOnlyList<DeficiencyCountByLabelDto> ByLifecycleStatus { get; set; } = Array.Empty<DeficiencyCountByLabelDto>();
    public IReadOnlyList<DeficiencyCountByLabelDto> ByDepartment { get; set; } = Array.Empty<DeficiencyCountByLabelDto>();
    public IReadOnlyList<DeficiencyCountByLabelDto> ByStage { get; set; } = Array.Empty<DeficiencyCountByLabelDto>();
}

public class DeficiencyCountByLabelDto
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}

/// <summary>Enriched deficiency row for command center grid.</summary>
public class DeficiencyWorkspaceItemDto
{
    public Guid Id { get; set; }
    public string DeficiencyCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DeficiencyType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string LifecycleStatus { get; set; } = string.Empty;
    public string MaturityLevel { get; set; } = string.Empty;
    public string? RootCauseCategory { get; set; }
    public string Classification { get; set; } = "ComplianceDeficiency";

    public Guid ObjectiveElementId { get; set; }
    public string ObjectiveElementCode { get; set; } = string.Empty;
    public string ObjectiveElementName { get; set; } = string.Empty;
    public string ChapterCode { get; set; } = string.Empty;
    public string ChapterName { get; set; } = string.Empty;
    public string StandardCode { get; set; } = string.Empty;
    public string StandardName { get; set; } = string.Empty;

    public Guid? PrimaryRemediationTaskId { get; set; }
    public string? PrimaryTaskTitle { get; set; }
    public string? PrimaryTaskStatus { get; set; }
    public string? AssignedOwner { get; set; }
    public string? Department { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? AssignedDate { get; set; }
    public DateTime? TargetResolutionDate { get; set; }
    public DateTime? ActualResolutionDate { get; set; }
    public bool IsOverdue { get; set; }

    // Sprint 10 SLA
    public string? SlaStatus { get; set; }
    public DateTime? SlaDueDate { get; set; }
    public int? DaysRemaining { get; set; }
    public double? SlaPercentConsumed { get; set; }
    public int? EscalationLevel { get; set; }
    public string? EscalationState { get; set; }

    // Sprint 11 / Phase 2 CAPA grid
    public string? CapaNumber { get; set; }
    public DateTime? OwnerAcceptedAt { get; set; }
    public string? OwnerAcceptedBy { get; set; }
    public bool OwnerAcceptanceRejected { get; set; }
    public string? CorrectiveActionStatus { get; set; }
    public string? PreventiveActionStatus { get; set; }
    public bool CapaOverdue { get; set; }
    public bool IsRecurringIssue { get; set; }
    public string? EffectivenessReviewStatus { get; set; }

    public int? RiskLikelihood { get; set; }
    public int? RiskImpact { get; set; }
    public int? RiskScore { get; set; }
    public string? AssessedRiskLevel { get; set; }

    public string EvidenceStatus { get; set; } = string.Empty;
    public string VerificationStatus { get; set; } = string.Empty;
    public int EvidenceCount { get; set; }
    public int RejectedEvidenceCount { get; set; }
    public int NeedsMoreEvidenceCount { get; set; }

    public bool BlocksCore { get; set; }
    public bool BlocksCommitment { get; set; }
    public bool BlocksAchievement { get; set; }
    public bool BlocksExcellence { get; set; }
    public string BlockingStageLabel { get; set; } = string.Empty;

    public DateTime DetectedDate { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ClosedDate { get; set; }
}

public class DeficiencyWorkspaceListDto
{
    public IReadOnlyList<DeficiencyWorkspaceItemDto> Items { get; set; } = Array.Empty<DeficiencyWorkspaceItemDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

/// <summary>Full detail for deficiency detail panel.</summary>
public class DeficiencyWorkspaceDetailDto : DeficiencyWorkspaceItemDto
{
    public string? RiskCategory { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? BlockingReason { get; set; }
    public DateTime LastEvaluatedDate { get; set; }
    public Guid ComplianceObligationId { get; set; }
    public string? LatestEvidenceTitle { get; set; }
    public DateTime? LatestEvidenceDate { get; set; }
    public string? OriginType { get; set; }
    public Guid? OriginFindingId { get; set; }
    public string? OriginFindingNumber { get; set; }
    public string? OriginFindingSource { get; set; }
    public DateTime? PromotedAt { get; set; }

    // Sprint 8 RCA
    public string? ProblemStatement { get; set; }
    public string? RootCauseSummary { get; set; }
    public string? ImpactDescription { get; set; }
    public string? CorrectiveActionSummary { get; set; }
    public string? PreventiveActionSummary { get; set; }
    public string? Why1 { get; set; }
    public string? Why2 { get; set; }
    public string? Why3 { get; set; }
    public string? Why4 { get; set; }
    public string? Why5 { get; set; }
    public string? FinalRootCause { get; set; }

    // Sprint 8 remediation planning
    public string? ActionPlan { get; set; }
    public string? ExpectedOutcome { get; set; }
    public DateTime? RemediationTargetCompletion { get; set; }
    public string? SuccessCriteria { get; set; }
    public string EffectivenessReviewStatus { get; set; } = "NotRequired";

    public IReadOnlyList<DeficiencyTimelineEntryDto> Timeline { get; set; } = Array.Empty<DeficiencyTimelineEntryDto>();

    // Sprint 10 SLA detail
    public Guid? SlaPolicyId { get; set; }
    public string? SlaPolicyName { get; set; }
    public DateTime? SlaStartDate { get; set; }
    public double? ElapsedHours { get; set; }
    public double? RemainingHours { get; set; }
    public double? OverdueHours { get; set; }
    public string? EscalationTargetUserId { get; set; }
    public string? EscalationTargetLabel { get; set; }
    public IReadOnlyList<DeficiencyEscalationHistoryItemDto> EscalationHistory { get; set; }
        = Array.Empty<DeficiencyEscalationHistoryItemDto>();

    // Sprint 11 CAPA detail
    public string? CorrectiveActionOwner { get; set; }
    public DateTime? CorrectiveActionStartDate { get; set; }
    public DateTime? CorrectiveActionTargetDate { get; set; }
    public DateTime? CorrectiveActionCompletedDate { get; set; }
    public bool CorrectiveActionOverdue { get; set; }
    public string? PreventiveActionOwner { get; set; }
    public DateTime? PreventiveActionTargetDate { get; set; }
    public DateTime? PreventiveActionCompletedDate { get; set; }
    public bool PreventiveActionOverdue { get; set; }
    public string? PreventiveActionSuccessCriteria { get; set; }
    public DateTime? EffectivenessReviewScheduledDate { get; set; }
    public int? EffectivenessReviewDaysOffset { get; set; }
    public DateTime? EffectivenessReviewDate { get; set; }
    public string? EffectivenessReviewer { get; set; }
    public string? EffectivenessReviewNotes { get; set; }
    public string? EffectivenessReviewOutcome { get; set; }
    public string? RecurrenceReason { get; set; }
    public DateTime? RecurrenceDetectedAt { get; set; }
    public IReadOnlyList<CapaLifecycleStepDto> CapaLifecycleSteps { get; set; } = Array.Empty<CapaLifecycleStepDto>();
}

public class DeficiencyEscalationHistoryItemDto
{
    public Guid Id { get; set; }
    public int Level { get; set; }
    public string? TargetUserId { get; set; }
    public string TargetLabel { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public DateTime TriggeredAt { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public class DeficiencyTimelineEntryDto
{
    public string Action { get; set; } = string.Empty;
    public string ActorType { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public string? DetailJson { get; set; }
}

public class PatchDeficiencyRootCauseRequestDto
{
    public string RootCauseCategory { get; set; } = string.Empty;
}

public class PatchDeficiencyRcaRequestDto
{
    public string? RootCauseCategory { get; set; }
    public string? ProblemStatement { get; set; }
    public string? RootCauseSummary { get; set; }
    public string? ImpactDescription { get; set; }
    public string? CorrectiveActionSummary { get; set; }
    public string? PreventiveActionSummary { get; set; }
    public string? Why1 { get; set; }
    public string? Why2 { get; set; }
    public string? Why3 { get; set; }
    public string? Why4 { get; set; }
    public string? Why5 { get; set; }
    public string? FinalRootCause { get; set; }
}

public class PatchDeficiencyRemediationRequestDto
{
    public int? RiskLikelihood { get; set; }
    public int? RiskImpact { get; set; }
    public string? Department { get; set; }
    public DateTime? TargetResolutionDate { get; set; }
    public string? ActionPlan { get; set; }
    public string? ExpectedOutcome { get; set; }
    public DateTime? RemediationTargetCompletion { get; set; }
    public string? SuccessCriteria { get; set; }
    public string? EffectivenessReviewStatus { get; set; }
}

public class CapaLifecycleStepDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool IsComplete { get; set; }
    public bool IsCurrent { get; set; }
}

public class PatchDeficiencyCorrectiveActionRequestDto
{
    public string? Summary { get; set; }
    public string? Owner { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? TargetDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string? Status { get; set; }
}

public class PatchDeficiencyPreventiveActionRequestDto
{
    public string? Summary { get; set; }
    public string? Owner { get; set; }
    public DateTime? TargetDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string? SuccessCriteria { get; set; }
    public string? Status { get; set; }
}

public class PatchDeficiencyEffectivenessReviewRequestDto
{
    public string? Status { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public int? ReviewDaysOffset { get; set; }
    public DateTime? ReviewDate { get; set; }
    public string? Reviewer { get; set; }
    public string? Notes { get; set; }
    public string? Outcome { get; set; }
}
