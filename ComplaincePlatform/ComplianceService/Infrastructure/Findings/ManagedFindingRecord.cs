using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Infrastructure.Findings;

public sealed class ManagedFindingRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ClauseReference { get; set; } = string.Empty;
    public FindingSeverity Severity { get; set; }
    public FindingStatus Status { get; set; }
    public FindingDepartment Department { get; set; }
    public FindingRaisedBy RaisedBy { get; set; }
    public DateTime DateRaised { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public FindingDepartment? AssignedDepartment { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastUpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? LastUpdatedBy { get; set; }
    public int EvidenceCount { get; set; }
}
