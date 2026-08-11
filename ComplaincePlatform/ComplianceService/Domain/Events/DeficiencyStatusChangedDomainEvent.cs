using ComplianceService.Domain.Enums;

namespace ComplianceService.Domain.Events;

/// <summary>
/// Raised when a ComplianceDeficiency status transitions to a new state.
/// </summary>
public class DeficiencyStatusChangedDomainEvent : IDomainEvent
{
    public Guid DeficiencyId { get; set; }
    public Guid HospitalId { get; set; }
    public Guid FrameworkId { get; set; }
    public DeficiencyStatus PreviousStatus { get; set; }
    public DeficiencyStatus NewStatus { get; set; }
    public string? ChangedBy { get; set; }
    public DateTime OccurredOn { get; set; } = DateTime.UtcNow;
}
