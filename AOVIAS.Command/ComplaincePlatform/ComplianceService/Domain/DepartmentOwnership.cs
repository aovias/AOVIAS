using ComplianceService.Domain.Enums;
using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain;

/// <summary>Sprint 9B — who owns a department inside an organization/hospital.</summary>
public class DepartmentOwnership : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid? HospitalId { get; set; }
    public Guid DepartmentId { get; set; }
    public string PrimaryOwnerUserId { get; set; } = string.Empty;
    public string? BackupOwnerUserId { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public DepartmentOwnershipStatus Status { get; set; } = DepartmentOwnershipStatus.Active;

    public ComplianceDepartment? Department { get; set; }
}
