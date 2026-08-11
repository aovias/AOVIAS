using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.DTOs;

/// <summary>Wire JSON uses camelCase enum values (see <see cref="Microsoft.AspNetCore.Mvc.JsonOptions"/> in Program.cs).</summary>
public record FindingListItemDto
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string ClauseReference { get; init; } = string.Empty;
    public FindingSeverity Severity { get; init; }
    public FindingStatus Status { get; init; }
    public FindingDepartment Department { get; init; }
    public FindingRaisedBy RaisedBy { get; init; }
    public DateTime DateRaised { get; init; }
    public Guid? AssignedToUserId { get; init; }
    public FindingDepartment? AssignedDepartment { get; init; }
    public DateTime? DueDate { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime LastUpdatedAt { get; init; }
    public Guid? CreatedBy { get; init; }
    public Guid? LastUpdatedBy { get; init; }
    public int EvidenceCount { get; init; }
    public ComplianceItemClassification Classification { get; init; } = ComplianceItemClassification.AuditFinding;
}

public record FindingDetailDto : FindingListItemDto;

public record CreateManagedFindingRequest
{
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string ClauseReference { get; init; } = string.Empty;
    public FindingSeverity Severity { get; init; }
    public FindingDepartment Department { get; init; }
    public FindingRaisedBy RaisedBy { get; init; } = FindingRaisedBy.InternalAudit;
    public FindingClassification? Classification { get; init; }
    public FindingStatus? Status { get; init; }
    public DateTime? DateRaised { get; init; }
    public Guid? ObjectiveElementId { get; init; }
    public DateTime? AuditDate { get; init; }
    public string? AuditorName { get; init; }
    public string? AuditCycle { get; init; }
    public string? Observation { get; init; }
    public string? Requirement { get; init; }
    public string? Impact { get; init; }
    public string? Recommendation { get; init; }
    public Guid? AuditSessionId { get; init; }
    public IReadOnlyList<CreateFindingAttachmentRequest>? Attachments { get; init; }
}

public record CreateFindingAttachmentRequest
{
    public string FileName { get; init; } = string.Empty;
    public string FileUrl { get; init; } = string.Empty;
    public string? ContentType { get; init; }
    public string AttachmentType { get; init; } = "Document";
    public long? FileSizeBytes { get; init; }
}

public record ManagedFindingListFilters
{
    public string? Search { get; init; }
    public FindingSeverity? Severity { get; init; }
    public FindingStatus? Status { get; init; }
    public FindingDepartment? Department { get; init; }
}
