using ComplianceService.Domain.Enums;

namespace ComplianceService.Application.DTOs;

public record DocumentationDashboardDto
{
    public int TotalDocuments { get; init; }
    public int Published { get; init; }
    public int Draft { get; init; }
    public int UnderReview { get; init; }
    public int Expired { get; init; }
    public int DueForReview { get; init; }
    public int PendingApprovals { get; init; }
    public int PendingAcknowledgements { get; init; }
    public int DepartmentCoverage { get; init; }
    public int OeCoverage { get; init; }
    public IReadOnlyList<ControlledDocumentListItemDto> RecentDocuments { get; init; } =
        Array.Empty<ControlledDocumentListItemDto>();
}

public record ControlledDocumentListItemDto
{
    public Guid Id { get; init; }
    public string DocumentNumber { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public ControlledDocumentType DocumentType { get; init; }
    public string? Department { get; init; }
    public string? OwnerName { get; init; }
    public string? ReviewerName { get; init; }
    public string? ApproverName { get; init; }
    public string? CurrentVersion { get; init; }
    public ControlledDocumentStatus Status { get; init; }
    public DateTime? EffectiveDate { get; init; }
    public DateTime? ReviewDate { get; init; }
    public DateTime? ExpiryDate { get; init; }
    public Guid? CurrentVersionId { get; init; }
    public string? MappedFramework { get; init; }
    public string? MappedChapter { get; init; }
    public string? MappedStandard { get; init; }
    public string? MappedObjectiveElement { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime LastUpdatedAt { get; init; }
}

public record ControlledDocumentDetailDto : ControlledDocumentListItemDto
{
    public Guid? HospitalId { get; init; }
    public Guid? OwnerUserId { get; init; }
    public Guid? ReviewerUserId { get; init; }
    public Guid? ApproverUserId { get; init; }
    public DateTime? NextReviewDate { get; init; }
    public int ReviewFrequencyMonths { get; init; }
    public string? ReviewOwnerName { get; init; }
    public Guid? MappedObjectiveElementId { get; init; }
    public string? Content { get; init; }
    public IReadOnlyList<DocumentVersionSummaryDto> Versions { get; init; } = Array.Empty<DocumentVersionSummaryDto>();
    public IReadOnlyList<DocumentApprovalDto> Approvals { get; init; } = Array.Empty<DocumentApprovalDto>();
    public IReadOnlyList<DocumentLifecycleAuditDto> AuditTrail { get; init; } = Array.Empty<DocumentLifecycleAuditDto>();
    public DocumentAcknowledgementStatsDto AcknowledgementStats { get; init; } = new();
}

public record DocumentVersionSummaryDto
{
    public Guid Id { get; init; }
    public int VersionNumber { get; init; }
    public string VersionLabel { get; init; } = string.Empty;
    public string? ChangeSummary { get; init; }
    public bool IsCurrent { get; init; }
    public bool IsSuperseded { get; init; }
    public string? CreatedByName { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record DocumentVersionDetailDto : DocumentVersionSummaryDto
{
    public string Content { get; init; } = string.Empty;
    public IReadOnlyList<DocumentApprovalDto> Approvals { get; init; } = Array.Empty<DocumentApprovalDto>();
}

public record DocumentApprovalDto
{
    public Guid Id { get; init; }
    public int ApprovalOrder { get; init; }
    public string ApproverRole { get; init; } = string.Empty;
    public string? ApproverName { get; init; }
    public DocumentApprovalStatus Status { get; init; }
    public DateTime? DecidedAt { get; init; }
    public string? Comments { get; init; }
}

public record DocumentLifecycleAuditDto
{
    public Guid Id { get; init; }
    public string EventType { get; init; } = string.Empty;
    public ControlledDocumentStatus? FromStatus { get; init; }
    public ControlledDocumentStatus? ToStatus { get; init; }
    public string? PerformedByName { get; init; }
    public string? Details { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record DocumentAcknowledgementStatsDto
{
    public int Total { get; init; }
    public int Read { get; init; }
    public int Acknowledged { get; init; }
    public int Pending { get; init; }
    public double CompletionPercent { get; init; }
}

public record DocumentAcknowledgementListItemDto
{
    public Guid Id { get; init; }
    public string DocumentNumber { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string AssigneeName { get; init; } = string.Empty;
    public DocumentAcknowledgementAssigneeType AssigneeType { get; init; }
    public DateTime? ReadAt { get; init; }
    public DateTime? AcknowledgedAt { get; init; }
    public int ReminderCount { get; init; }
}

public record ReviewCalendarItemDto
{
    public Guid DocumentId { get; init; }
    public string DocumentNumber { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public DateTime? NextReviewDate { get; init; }
    public DateTime? EffectiveDate { get; init; }
    public string? ReviewOwnerName { get; init; }
    public int ReviewFrequencyMonths { get; init; }
    public ControlledDocumentStatus Status { get; init; }
    public string ReminderTier { get; init; } = string.Empty;
}

public record DocumentTemplateDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public ControlledDocumentType DocumentType { get; init; }
    public string? Description { get; init; }
    public bool IsBuiltIn { get; init; }
    public string ContentTemplate { get; init; } = string.Empty;
}

public record DocumentationAnalyticsDto
{
    public int PoliciesDueReview { get; init; }
    public int ExpiredDocuments { get; init; }
    public double AcknowledgementPercent { get; init; }
    public int DepartmentComplianceCount { get; init; }
    public IReadOnlyList<MostRevisedDocumentDto> MostRevisedSops { get; init; } = Array.Empty<MostRevisedDocumentDto>();
    public int UnmappedDocuments { get; init; }
    public double ReviewCyclePerformancePercent { get; init; }
    public double ApprovalTurnaroundDays { get; init; }
    public double FrameworkCoveragePercent { get; init; }
}

public record MostRevisedDocumentDto
{
    public Guid Id { get; init; }
    public string DocumentNumber { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public int RevisionCount { get; init; }
}

public record CreateControlledDocumentRequest
{
    public string Title { get; init; } = string.Empty;
    public ControlledDocumentType DocumentType { get; init; } = ControlledDocumentType.Policy;
    public string? Department { get; init; }
    public string? OwnerName { get; init; }
    public string Content { get; init; } = string.Empty;
    public Guid? TemplateId { get; init; }
    public string? MappedFramework { get; init; }
    public string? MappedChapter { get; init; }
    public string? MappedStandard { get; init; }
    public Guid? MappedObjectiveElementId { get; init; }
    public string? MappedObjectiveElement { get; init; }
}

public record UpdateControlledDocumentRequest
{
    public string? Title { get; init; }
    public string? Department { get; init; }
    public string? OwnerName { get; init; }
    public string? ReviewerName { get; init; }
    public string? ApproverName { get; init; }
    public DateTime? EffectiveDate { get; init; }
    public DateTime? ReviewDate { get; init; }
    public DateTime? ExpiryDate { get; init; }
    public int? ReviewFrequencyMonths { get; init; }
    public string? ReviewOwnerName { get; init; }
    public string? MappedFramework { get; init; }
    public string? MappedChapter { get; init; }
    public string? MappedStandard { get; init; }
    public Guid? MappedObjectiveElementId { get; init; }
    public string? MappedObjectiveElement { get; init; }
}

public record CreateDocumentVersionRequest
{
    public string Content { get; init; } = string.Empty;
    public string? ChangeSummary { get; init; }
}

public record TransitionDocumentStatusRequest
{
    public ControlledDocumentStatus TargetStatus { get; init; }
    public string? Comments { get; init; }
}

public record ApproveDocumentRequest
{
    public Guid DocumentVersionId { get; init; }
    public bool Approved { get; init; } = true;
    public string? Comments { get; init; }
}

public record PublishDocumentRequest
{
    public DateTime? EffectiveDate { get; init; }
    public DateTime? ExpiryDate { get; init; }
    /// <summary>When true, clears prior acknowledgements so assignees must re-ack the published version.</summary>
    public bool RequireReAcknowledge { get; init; } = true;
    public IReadOnlyList<AcknowledgementAssignmentDto>? Assignments { get; init; }
}

public record CompleteDocumentReviewRequest
{
    public string? Outcome { get; init; }
    public string? Notes { get; init; }
    /// <summary>When true (default), return to Effective with a new next-review date. When false, move to UnderReview for revision.</summary>
    public bool NoChangesRequired { get; init; } = true;
    public DateTime? NextReviewDate { get; init; }
}

public record AcknowledgementAssignmentDto
{
    public DocumentAcknowledgementAssigneeType AssigneeType { get; init; }
    public string AssigneeKey { get; init; } = string.Empty;
    public string AssigneeName { get; init; } = string.Empty;
    public Guid? AssigneeUserId { get; init; }
}

public record AcknowledgeDocumentRequest
{
    public Guid AcknowledgementId { get; init; }
    public bool MarkReadOnly { get; init; }
}

public record VersionCompareDto
{
    public DocumentVersionDetailDto Current { get; init; } = null!;
    public DocumentVersionDetailDto? Previous { get; init; }
}
