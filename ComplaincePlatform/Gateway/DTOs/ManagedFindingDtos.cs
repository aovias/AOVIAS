using Shared.Infrastructure.Domain.Enums;

namespace Gateway.DTOs;

/// <summary>Aligned with ComplianceService CreateManagedFindingRequest.</summary>
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
}
