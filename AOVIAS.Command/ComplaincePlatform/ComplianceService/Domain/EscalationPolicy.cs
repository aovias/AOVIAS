using ComplianceService.Domain.Enums;
using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain;

/// <summary>Sprint 10 — hospital/org escalation hierarchy for a severity band.</summary>
public class EscalationPolicy : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid? HospitalId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DeficiencySeverity? Severity { get; set; }
    public SlaPolicyStatus Status { get; set; } = SlaPolicyStatus.Active;
    public DateTime CreatedOn { get; set; }

    public ICollection<EscalationLevel> Levels { get; set; } = new List<EscalationLevel>();
}
