using ComplianceService.Domain.Enums;

namespace ComplianceService.Domain.Events;

/// <summary>
/// Raised when a ComplianceDeficiency is closed — either verified-resolved or
/// auto-closed by the evaluation engine when the underlying obligation state is corrected.
/// Routes through the Outbox to trigger snapshot recalculation.
/// </summary>
public class DeficiencyClosedDomainEvent : IDomainEvent
{
    public Guid DeficiencyId { get; set; }
    public Guid HospitalId { get; set; }
    public Guid FrameworkId { get; set; }
    public DeficiencyType DeficiencyType { get; set; }
    public string? ClosedBy { get; set; }
    public DateTime OccurredOn { get; set; } = DateTime.UtcNow;
}
