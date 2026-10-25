namespace ComplianceService.Application.DTOs;

public class FindingsWorkspaceDashboardDto
{
    public int TotalFindings { get; set; }
    public int OpenFindings { get; set; }
    public int AcceptedFindings { get; set; }
    public int ClosedFindings { get; set; }
    public int CriticalFindings { get; set; }
    public int DraftFindings { get; set; }
    public int AwaitingReview { get; set; }
    public int EligibleForPromotion { get; set; }
    public int PromotedFindings { get; set; }
    public int FindingsAwaitingPromotion { get; set; }
    public decimal PromotionRate { get; set; }
    public IReadOnlyList<FindingsCountByLabelDto> BySource { get; set; } = Array.Empty<FindingsCountByLabelDto>();
    public IReadOnlyList<FindingsCountByLabelDto> ByDepartment { get; set; } = Array.Empty<FindingsCountByLabelDto>();
    public IReadOnlyList<FindingsCountByLabelDto> ByChapter { get; set; } = Array.Empty<FindingsCountByLabelDto>();
    public IReadOnlyList<FindingsCountByLabelDto> ByClassification { get; set; } = Array.Empty<FindingsCountByLabelDto>();
}

public class FindingsCountByLabelDto
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class FindingsWorkspaceItemDto
{
    public Guid Id { get; set; }
    public string FindingNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Classification { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string? Owner { get; set; }
    public string ChapterCode { get; set; } = string.Empty;
    public string ChapterName { get; set; } = string.Empty;
    public string StandardCode { get; set; } = string.Empty;
    public string StandardName { get; set; } = string.Empty;
    public string ObjectiveElementCode { get; set; } = string.Empty;
    public string ObjectiveElementName { get; set; } = string.Empty;
    public Guid? ObjectiveElementId { get; set; }
    public string ClauseReference { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public DateTime LastUpdated { get; set; }
    public int AttachmentCount { get; set; }
    public int CommentCount { get; set; }
    public bool IsPromoted { get; set; }
    public string PromotionReadiness { get; set; } = string.Empty;
}

public class FindingsWorkspaceListDto
{
    public IReadOnlyList<FindingsWorkspaceItemDto> Items { get; set; } = Array.Empty<FindingsWorkspaceItemDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class FindingCommentDto
{
    public Guid Id { get; set; }
    public Guid AuthorUserId { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class FindingAttachmentDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public string AttachmentType { get; set; } = string.Empty;
    public long? FileSizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }
}

public class FindingHistoryEntryDto
{
    public string Action { get; set; } = string.Empty;
    public Guid? ActorUserId { get; set; }
    public string ActorType { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public string? Detail { get; set; }
}

public class FindingLifecycleStepDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool IsComplete { get; set; }
    public bool IsCurrent { get; set; }
    public DateTime? OccurredAt { get; set; }
    public Guid? ActorUserId { get; set; }
}

public class FindingsWorkspaceDetailDto : FindingsWorkspaceItemDto
{
    public IReadOnlyList<FindingCommentDto> Comments { get; set; } = Array.Empty<FindingCommentDto>();
    public IReadOnlyList<FindingAttachmentDto> Attachments { get; set; } = Array.Empty<FindingAttachmentDto>();
    public IReadOnlyList<FindingHistoryEntryDto> History { get; set; } = Array.Empty<FindingHistoryEntryDto>();
    public IReadOnlyList<FindingLifecycleStepDto> LifecycleSteps { get; set; } = Array.Empty<FindingLifecycleStepDto>();
    public Guid? PromotedDeficiencyId { get; set; }
    public DateTime? AuditDate { get; set; }
    public string? AuditorName { get; set; }
    public string? AuditCycle { get; set; }
    public string? Observation { get; set; }
    public string? Requirement { get; set; }
    public string? Impact { get; set; }
    public string? Recommendation { get; set; }
    public DateTime DateRaised { get; set; }
}

public class FindingComplianceContextOptionDto
{
    public Guid ObjectiveElementId { get; set; }
    public string ObjectiveElementCode { get; set; } = string.Empty;
    public string ObjectiveElementName { get; set; } = string.Empty;
    public string StandardCode { get; set; } = string.Empty;
    public string StandardName { get; set; } = string.Empty;
    public string ChapterCode { get; set; } = string.Empty;
    public string ChapterName { get; set; } = string.Empty;
}

public class AddFindingCommentRequestDto
{
    public string Body { get; set; } = string.Empty;
}

public class AddFindingAttachmentRequestDto
{
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public string AttachmentType { get; set; } = "Document";
    public long? FileSizeBytes { get; set; }
}

public class PatchFindingStatusRequestDto
{
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// DEF-M3-001 — classifies an existing finding. Standard and chapter are resolved server-side from
/// the objective element, so the operator supplies only the objective element.
/// </summary>
public class PatchFindingComplianceContextRequestDto
{
    public Guid ObjectiveElementId { get; set; }
}
