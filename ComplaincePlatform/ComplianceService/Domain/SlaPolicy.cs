using ComplianceService.Domain.Enums;
using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain;

/// <summary>Sprint 10 — configurable SLA policy per severity/stage.</summary>
public class SlaPolicy : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid? HospitalId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DeficiencySeverity Severity { get; set; }
    public ObjectiveElementClassification? LifecycleStage { get; set; }
    public int TargetResolutionHours { get; set; }
    public int WarningThresholdPercent { get; set; } = 80;
    public Guid? EscalationPolicyId { get; set; }
    public SlaPolicyStatus Status { get; set; } = SlaPolicyStatus.Active;
    public DateTime CreatedOn { get; set; }

    public EscalationPolicy? EscalationPolicy { get; set; }
}
