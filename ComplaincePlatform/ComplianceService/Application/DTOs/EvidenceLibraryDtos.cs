namespace ComplianceService.Application.DTOs;

public sealed class EvidenceSummaryDto
{
    public int Total { get; init; }
    public int Approved { get; init; }
    public int PendingReview { get; init; }
    public int Rejected { get; init; }
    public int Missing { get; init; }
}

public sealed class PagedResultDto<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

public sealed class EvidenceListItemDto
{
    public Guid Id { get; init; }
    public Guid EvidenceId { get; init; }
    public Guid ObjectiveElementId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public long FileSizeBytes { get; init; }
    public string FileType { get; init; } = string.Empty;
    public string ObjectiveElementCode { get; init; } = string.Empty;
    public string ObjectiveElementName { get; init; } = string.Empty;
    public string ChapterCode { get; init; } = string.Empty;
    public string ChapterName { get; init; } = string.Empty;
    /// <summary>Task department id when evidence is linked to a task; use with master department lookup when <see cref="Department"/> is empty.</summary>
    public Guid? DepartmentId { get; init; }
    public string Department { get; init; } = string.Empty;
    public string UploadedBy { get; init; } = string.Empty;
    public DateTime UploadDate { get; init; }
    /// <summary>approved | pendingReview | rejected</summary>
    public string Status { get; init; } = string.Empty;
    public bool? IsApproved { get; init; }
    public string? ReviewComment { get; init; }
    /// <summary>Base64-encoded row version from ObjectiveElementEvidence for optimistic concurrency.</summary>
    public string? RowVersion { get; init; }
    public string? VerifiedBy { get; init; }
    public DateTime? VerifiedAt { get; init; }
    public string? LinkedTask { get; init; }
    public string? Description { get; init; }
    public int Version { get; init; }
}

public sealed class EvidenceAnalyticsDto
{
    public IReadOnlyList<EvidenceChapterAggregateDto> ByChapter { get; init; } = Array.Empty<EvidenceChapterAggregateDto>();
    public IReadOnlyList<EvidenceStandardAggregateDto> ByStandard { get; init; } = Array.Empty<EvidenceStandardAggregateDto>();
    public IReadOnlyList<EvidenceDepartmentAggregateDto> ByDepartment { get; init; } = Array.Empty<EvidenceDepartmentAggregateDto>();
    public EvidenceApprovalStatusDto ApprovalStatus { get; init; } = new();
    public IReadOnlyList<EvidenceMissingItemDto> MissingEvidence { get; init; } = Array.Empty<EvidenceMissingItemDto>();
}

public sealed class EvidenceChapterAggregateDto
{
    public Guid? ChapterId { get; init; }
    public string Name { get; init; } = string.Empty;
    public int Count { get; init; }
}

public sealed class EvidenceStandardAggregateDto
{
    public Guid? StandardId { get; init; }
    /// <summary>Parent chapter for scoping filter options when a chapter filter is active.</summary>
    public Guid? ChapterId { get; init; }
    public string Name { get; init; } = string.Empty;
    public int Count { get; init; }
}

public sealed class EvidenceDepartmentAggregateDto
{
    public Guid? DepartmentId { get; init; }
    public string Name { get; init; } = string.Empty;
    public int Count { get; init; }
}

public sealed class EvidenceApprovalStatusDto
{
    public int Approved { get; init; }
    public int PendingReview { get; init; }
    public int Rejected { get; init; }
}

public sealed class EvidenceMissingItemDto
{
    public Guid ObjectiveElementId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Chapter { get; init; } = string.Empty;
}

public sealed class EvidenceActivityItemDto
{
    public string User { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string Entity { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// Evidence-centric lifecycle row (Upload -> Updated -> Review) for the grouped timeline view.
/// One row per evidence, sorted server-side by <see cref="MostRecentAt"/> DESC.
/// </summary>
public sealed class EvidenceTimelineItemDto
{
    public Guid EvidenceId { get; init; }
    public Guid ObjectiveElementId { get; init; }
    public string ObjectiveElementCode { get; init; } = string.Empty;
    public string ObjectiveElementName { get; init; } = string.Empty;
    public string ChapterCode { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;
    public string FileType { get; init; } = string.Empty;
    public string? FileUrl { get; init; }

    public string UploadedBy { get; init; } = string.Empty;
    public DateTime UploadedAt { get; init; }

    /// <summary>Latest <see cref="EvidenceVersion.CreatedBy"/> (falls back to UploadedBy when only v1 exists).</summary>
    public string UpdatedBy { get; init; } = string.Empty;

    /// <summary>Max <c>EvidenceVersion.CreatedAt</c>; equals <see cref="UploadedAt"/> when only v1 exists.</summary>
    public DateTime UpdatedAt { get; init; }

    /// <summary>approved | pendingReview | rejected</summary>
    public string Status { get; init; } = string.Empty;
    public string? VerifiedBy { get; init; }
    public DateTime? VerifiedAt { get; init; }

    public string? ReviewComment { get; init; }

    /// <summary>True when <see cref="UpdatedAt"/> is strictly greater than <see cref="UploadedAt"/>.</summary>
    public bool HasUpdate { get; init; }

    /// <summary>Computed server-side as MAX(UploadedAt, UpdatedAt, VerifiedAt) for stable ordering.</summary>
    public DateTime MostRecentAt { get; init; }
}

/// <summary>Resolves active journey for multipart evidence upload from the Evidence Library screen.</summary>
public sealed class EvidenceUploadContextDto
{
    /// <summary>Null when no journey exists for hospital + framework.</summary>
    public Guid? JourneyId { get; init; }
}

public sealed class EvidenceDashboardDto
{
    public EvidenceSummaryDto Summary { get; init; } = new();
    public EvidenceAnalyticsDto Analytics { get; init; } = new();
    public IReadOnlyList<EvidenceActivityItemDto> RecentActivity { get; init; } = Array.Empty<EvidenceActivityItemDto>();

    /// <summary>Grouped evidence-lifecycle rows for the new Recent Activity timeline view.</summary>
    public IReadOnlyList<EvidenceTimelineItemDto> RecentTimeline { get; init; } = Array.Empty<EvidenceTimelineItemDto>();
}
