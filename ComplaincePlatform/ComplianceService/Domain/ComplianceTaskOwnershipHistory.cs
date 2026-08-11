using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain;

/// <summary>
/// Audit trail of ComplianceTask assignee changes (reassignment / delegation).
/// </summary>
public class ComplianceTaskOwnershipHistory : BaseEntity
{
    public Guid TaskId { get; set; }
    public Guid HospitalId { get; set; }
    public string? PreviousAssigneeUserId { get; set; }
    public string NewAssigneeUserId { get; set; } = string.Empty;
    public Guid ChangedByUserId { get; set; }
    public string? Reason { get; set; }
    public DateTime ChangedAt { get; set; }
}
