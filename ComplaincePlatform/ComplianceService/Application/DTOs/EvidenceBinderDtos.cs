namespace ComplianceService.Application.DTOs;

/// <summary>Evidence Binder summary KPIs (objective-element scoped, applicable + active framework).</summary>
public sealed class EvidenceBinderSummaryDto
{
    public int TotalObjectives { get; init; }
    public int ObjectivesWithEvidence { get; init; }
    public int ObjectivesMissingEvidence { get; init; }
    /// <summary>Objective elements with at least one verified (approved) evidence link.</summary>
    public int ObjectivesApproved { get; init; }
    /// <summary>Count of OE–evidence links in Verified state.</summary>
    public int ApprovedEvidenceCount { get; init; }
    /// <summary>0–100: ObjectivesApproved / TotalObjectives.</summary>
    public int ReadinessPercent { get; init; }
}

public sealed class EvidenceBinderChapterDto
{
    public Guid ChapterId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int ObjectiveCount { get; init; }
    public int ObjectivesWithEvidence { get; init; }
    public int CoveragePercent { get; init; }
    /// <summary>low | medium | high (UI: green / yellow / red).</summary>
    public string RiskLevel { get; init; } = "low";
}

public sealed class EvidenceBinderObjectiveElementDto
{
    public Guid ObjectiveElementId { get; init; }
    public Guid ChapterId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int EvidenceCount { get; init; }
    public int TaskCount { get; init; }
    /// <summary>missing | partial | approved</summary>
    public string Status { get; init; } = "missing";
}

public sealed class EvidenceBinderOverviewDto
{
    public Guid ObjectiveElementId { get; init; }
    public Guid ChapterId { get; init; }
    public string ChapterCode { get; init; } = string.Empty;
    public string ChapterName { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int EvidenceCount { get; init; }
    public int TaskCount { get; init; }
    public string Status { get; init; } = "missing";
}

public sealed class EvidenceBinderEvidenceItemDto
{
    public Guid Id { get; init; }
    public Guid EvidenceId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string FileUrl { get; init; } = string.Empty;
    public string FileType { get; init; } = string.Empty;
    public long FileSizeBytes { get; init; }
    public string UploadedBy { get; init; } = string.Empty;
    public DateTime UploadedAt { get; init; }
    /// <summary>approved | pendingReview | rejected</summary>
    public string Status { get; init; } = string.Empty;
    public bool? IsApproved { get; init; }
    public string? ReviewComment { get; init; }
    public string? RowVersion { get; init; }
    public Guid? VerifiedBy { get; init; }
    public DateTime? VerifiedAt { get; init; }
    public int Version { get; init; }
}

/// <summary>Phase 1: empty until required-evidence templates or OE wizard flags are wired. Phase 2: optional PATCH persistence TBD.</summary>
public sealed class EvidenceBinderChecklistItemDto
{
    public Guid Id { get; init; }
    public Guid ObjectiveElementId { get; init; }
    public string Item { get; init; } = string.Empty;
    public bool IsCompleted { get; init; }
}

/// <summary>AI mapping suggestions for binder OE. Always advisory — human approval required.</summary>
public sealed class EvidenceBinderAiMappingItemDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Detail { get; init; }
    /// <summary>0–100 suggestion confidence when sourced from AI.</summary>
    public int ConfidencePercent { get; init; }
    public string? Reasoning { get; init; }
    public string? SourceReference { get; init; }
    /// <summary>Always true for AI mapping — never auto-applies links.</summary>
    public bool HumanApprovalRequired { get; init; } = true;
}
