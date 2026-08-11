using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Domain;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.Services;

public sealed class FindingAdapter : IFindingAdapter
{
    public AuditFindingDto ToAuditDto(Finding finding)
    {
        return new AuditFindingDto
        {
            Id = finding.Id,
            JourneyId = finding.ComplianceJourneyId ?? Guid.Empty,
            ObjectiveElementId = finding.ObjectiveElementId ?? Guid.Empty,
            Type = finding.Type ?? NonConformanceType.Observation,
            Description = finding.Description,
            DueDate = finding.DueDate ?? finding.DateRaised,
            IsResolved = finding.IsResolved,
            ResolvedAt = finding.ResolvedAt
        };
    }

    public FindingListItemDto ToManagedListDto(Finding finding)
    {
        return new FindingListItemDto
        {
            Id = finding.Id,
            OrganizationId = finding.TenantId,
            Title = finding.Title,
            Description = finding.Description,
            ClauseReference = finding.ClauseReference,
            Severity = finding.Severity,
            Status = finding.Status,
            Department = finding.Department,
            RaisedBy = finding.RaisedBy,
            DateRaised = finding.DateRaised,
            AssignedToUserId = finding.AssignedToUserId,
            AssignedDepartment = finding.AssignedDepartment,
            DueDate = finding.DueDate,
            CreatedAt = finding.CreatedAt,
            LastUpdatedAt = finding.LastUpdatedAt,
            CreatedBy = finding.CreatedByUserId,
            LastUpdatedBy = finding.LastUpdatedBy,
            EvidenceCount = finding.EvidenceCount
        };
    }

    public FindingDetailDto ToManagedDetailDto(Finding finding)
    {
        return new FindingDetailDto
        {
            Id = finding.Id,
            OrganizationId = finding.TenantId,
            Title = finding.Title,
            Description = finding.Description,
            ClauseReference = finding.ClauseReference,
            Severity = finding.Severity,
            Status = finding.Status,
            Department = finding.Department,
            RaisedBy = finding.RaisedBy,
            DateRaised = finding.DateRaised,
            AssignedToUserId = finding.AssignedToUserId,
            AssignedDepartment = finding.AssignedDepartment,
            DueDate = finding.DueDate,
            CreatedAt = finding.CreatedAt,
            LastUpdatedAt = finding.LastUpdatedAt,
            CreatedBy = finding.CreatedByUserId,
            LastUpdatedBy = finding.LastUpdatedBy,
            EvidenceCount = finding.EvidenceCount
        };
    }
}
