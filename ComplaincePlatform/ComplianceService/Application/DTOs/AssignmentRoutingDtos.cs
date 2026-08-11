namespace ComplianceService.Application.DTOs;

using Shared.Governance;

public class DepartmentOwnershipDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? HospitalId { get; set; }
    public Guid DepartmentId { get; set; }
    public string DepartmentCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string PrimaryOwnerUserId { get; set; } = string.Empty;
    public string? BackupOwnerUserId { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string Status { get; set; } = string.Empty;
    public IReadOnlyList<GovernanceOwnerDto>? GovernanceOwners { get; set; }
}

public class UpsertDepartmentOwnershipRequestDto
{
    public Guid OrganizationId { get; set; }
    public Guid? HospitalId { get; set; }
    public Guid DepartmentId { get; set; }
    public string PrimaryOwnerUserId { get; set; } = string.Empty;
    public string? BackupOwnerUserId { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public string? Status { get; set; }
}

public class DepartmentLabelMappingDto
{
    public string OeDepartmentLabel { get; set; } = string.Empty;
    public string NormalizedCode { get; set; } = string.Empty;
    public string DefaultResponsibleRole { get; set; } = string.Empty;
}
