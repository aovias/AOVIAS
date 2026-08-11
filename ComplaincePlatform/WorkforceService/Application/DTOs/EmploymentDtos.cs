namespace WorkforceService.Application.DTOs;

public class EmploymentDto
{
    public Guid Id { get; set; }
    public string EmploymentNumber { get; set; } = string.Empty;
    public Guid PersonId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid HospitalId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? OrganizationMemberId { get; set; }
    public Guid? EmploymentTypeValueId { get; set; }
    public Guid? EmploymentStatusValueId { get; set; }
    public string? EmploymentStatusName { get; set; }
    public string PersonSearchName { get; set; } = string.Empty;
    public DateTime? JoiningDate { get; set; }
    public DateTime? ConfirmationDate { get; set; }
    public DateTime? ProbationEndDate { get; set; }
    public DateTime? SeparationDate { get; set; }
    public int? NoticePeriodDays { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<PositionAssignmentDto> PositionAssignments { get; set; } = new();
    public List<DepartmentAssignmentDto> DepartmentAssignments { get; set; } = new();
    public List<ReportingRelationshipDto> ReportingRelationships { get; set; } = new();
    public List<EmploymentContractDto> Contracts { get; set; } = new();
    public AvailabilityProfileDto? Availability { get; set; }
}

public class EmploymentListItemDto
{
    public Guid Id { get; set; }
    public string EmploymentNumber { get; set; } = string.Empty;
    public string PersonSearchName { get; set; } = string.Empty;
    public Guid PersonId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid HospitalId { get; set; }
    public string? StatusName { get; set; }
    public string? PrimaryPositionName { get; set; }
    public DateTime? JoiningDate { get; set; }
}

public class PositionAssignmentDto
{
    public Guid Id { get; set; }
    public Guid PositionId { get; set; }
    public string? PositionName { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; }
}

public class DepartmentAssignmentDto
{
    public Guid Id { get; set; }
    public Guid MasterDepartmentId { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsPrimary { get; set; }
    public decimal AllocationPercentage { get; set; }
    public bool IsTemporary { get; set; }
    public bool IsActive { get; set; }
}

public class ReportingRelationshipDto
{
    public Guid Id { get; set; }
    public Guid ManagerEmploymentId { get; set; }
    public string? ManagerPersonSearchName { get; set; }
    public Guid? RelationshipTypeValueId { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
}

public class EmploymentContractDto
{
    public Guid Id { get; set; }
    public Guid? ContractTypeValueId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class AvailabilityProfileDto
{
    public string? WorkingDaysJson { get; set; }
    public string? WorkingHoursJson { get; set; }
    public string? ShiftPlaceholder { get; set; }
    public string? LeaveStatus { get; set; }
}

public class EmploymentHistoryDto
{
    public Guid Id { get; set; }
    public DateTime OccurredAt { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string? PreviousValueJson { get; set; }
    public string? NewValueJson { get; set; }
}

public class WorkforceTimelineDto
{
    public Guid Id { get; set; }
    public DateTime OccurredAt { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
}

public class LookupValueDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class PositionDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? HospitalId { get; set; }
    public string PositionCode { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public bool IsClinical { get; set; }
    public bool IsAdministrative { get; set; }
    public bool IsLeadership { get; set; }
}

public class OrgChartNodeDto
{
    public Guid EmploymentId { get; set; }
    public string EmploymentNumber { get; set; } = string.Empty;
    public string PersonSearchName { get; set; } = string.Empty;
    public string? PositionName { get; set; }
    public List<OrgChartNodeDto> Reports { get; set; } = new();
}

public class CreateEmploymentRequest
{
    public Guid PersonId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid HospitalId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? OrganizationMemberId { get; set; }
    public Guid? EmploymentTypeValueId { get; set; }
    public Guid? EmploymentStatusValueId { get; set; }
    public DateTime? JoiningDate { get; set; }
    public Guid? CreatedByUserId { get; set; }
}

public class UpdateEmploymentRequest : CreateEmploymentRequest
{
    public DateTime? ConfirmationDate { get; set; }
    public DateTime? ProbationEndDate { get; set; }
    public DateTime? SeparationDate { get; set; }
    public int? NoticePeriodDays { get; set; }
}

public class AssignPositionRequest
{
    public Guid PositionId { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsPrimary { get; set; } = true;
    public Guid? ActorUserId { get; set; }
}

public class AssignDepartmentRequest
{
    public Guid MasterDepartmentId { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsPrimary { get; set; }
    public decimal AllocationPercentage { get; set; } = 100;
    public bool IsTemporary { get; set; }
    public Guid? ActorUserId { get; set; }
}

public class AssignManagerRequest
{
    public Guid ManagerEmploymentId { get; set; }
    public Guid? RelationshipTypeValueId { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public Guid? ActorUserId { get; set; }
}

public class TransferEmploymentRequest
{
    public string TransferKind { get; set; } = "Department";
    public Guid? TargetOrganizationId { get; set; }
    public Guid? TargetHospitalId { get; set; }
    public Guid? TargetMasterDepartmentId { get; set; }
    public Guid? TargetPositionId { get; set; }
    public DateTime EffectiveDate { get; set; }
    public Guid? ReasonValueId { get; set; }
    public string? Notes { get; set; }
    public Guid? ActorUserId { get; set; }
}

public class PromoteEmploymentRequest
{
    public Guid NewPositionId { get; set; }
    public DateTime EffectiveDate { get; set; }
    public Guid? ReasonValueId { get; set; }
    public string? Notes { get; set; }
    public Guid? GradeValueId { get; set; }
    public Guid? BandValueId { get; set; }
    public Guid? ActorUserId { get; set; }
}

public class PagedResultDto<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
