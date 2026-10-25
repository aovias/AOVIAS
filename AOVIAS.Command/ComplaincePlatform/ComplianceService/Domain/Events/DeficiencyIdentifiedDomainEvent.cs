using ComplianceService.Domain.Enums;

namespace ComplianceService.Domain.Events;

/// <summary>
/// Raised when a new ComplianceDeficiency is identified by the evaluation engine.
/// Routes through the Outbox to trigger snapshot recalculation.
/// </summary>
public class DeficiencyIdentifiedDomainEvent : IDomainEvent
{
    public Guid DeficiencyId { get; set; }
    public Guid ObligationId { get; set; }
    public Guid HospitalId { get; set; }
    public Guid FrameworkId { get; set; }
    public DeficiencyType DeficiencyType { get; set; }
    public DeficiencySeverity Severity { get; set; }
    public DateTime OccurredOn { get; set; } = DateTime.UtcNow;
}
