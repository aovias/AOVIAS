using ComplianceService.Domain.Enums;
using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain;

/// <summary>
/// Hospital-scoped ownership assignment for an objective element (dept + primary owner).
/// </summary>
public class ObjectiveElementOwnership : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid HospitalId { get; set; }
    public Guid FrameworkId { get; set; }
    public Guid ObjectiveElementId { get; set; }
    public Guid DepartmentId { get; set; }
    public string PrimaryOwnerUserId { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public ObjectiveElementOwnershipStatus Status { get; set; } = ObjectiveElementOwnershipStatus.Active;
    public DateTime UpdatedAt { get; set; }

    public ObjectiveElement? ObjectiveElement { get; set; }
    public ComplianceDepartment? Department { get; set; }
}
