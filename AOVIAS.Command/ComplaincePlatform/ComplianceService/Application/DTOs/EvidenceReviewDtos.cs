namespace ComplianceService.Application.DTOs;

public sealed class EvidenceReviewQueueItemDto
{
    public Guid LinkId { get; init; }
    public Guid EvidenceId { get; init; }
    public Guid ObjectiveElementId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string FileType { get; init; } = string.Empty;
    public string ObjectiveElementCode { get; init; } = string.Empty;
    public string ObjectiveElementName { get; init; } = string.Empty;
    public string ChapterCode { get; init; } = string.Empty;
    public string ChapterName { get; init; } = string.Empty;
    public string? MaturityStage { get; init; }
    public Guid? DepartmentId { get; init; }
    public string Department { get; init; } = string.Empty;
    public string UploadedBy { get; init; } = string.Empty;
    public DateTime UploadDate { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? EvidenceType { get; init; }
    public Guid? TaskId { get; init; }
    public string? LinkedTask { get; init; }
    public Guid? OwnerId { get; init; }
    public string? RowVersion { get; init; }
    public int Version { get; init; }
}

public sealed class EvidenceReviewAuditEntryDto
{
    public Guid Id { get; init; }
    public Guid EvidenceId { get; init; }
    public Guid ObjectiveElementId { get; init; }
    public Guid ReviewerId { get; init; }
    public string? ReviewerRole { get; init; }
    public DateTime ReviewedAt { get; init; }
    public string Decision { get; init; } = string.Empty;
    public string PreviousStatus { get; init; } = string.Empty;
    public string NewStatus { get; init; } = string.Empty;
    public string? Comment { get; init; }
    public string? Recommendation { get; init; }
    public int EvidenceVersion { get; init; }
}
