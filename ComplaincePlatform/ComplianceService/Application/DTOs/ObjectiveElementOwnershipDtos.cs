namespace ComplianceService.Application.DTOs;

public sealed class ObjectiveElementOwnershipMatrixItemDto
{
    public Guid? OwnershipId { get; set; }
    public Guid ObjectiveElementId { get; set; }
    public string ObjectiveElementCode { get; set; } = string.Empty;
    public string ObjectiveElementTitle { get; set; } = string.Empty;
    public Guid FrameworkId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string? PrimaryOwnerUserId { get; set; }
    public DateTime? DueDate { get; set; }
    public string Status { get; set; } = "Unassigned";
    public string? JourneyStatus { get; set; }
}

public sealed class UpsertObjectiveElementOwnershipRequestDto
{
    public Guid OrganizationId { get; set; }
    public Guid FrameworkId { get; set; }
    public Guid ObjectiveElementId { get; set; }
    public Guid DepartmentId { get; set; }
    public string PrimaryOwnerUserId { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public string? Status { get; set; }
}
