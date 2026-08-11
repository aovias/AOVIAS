using ComplianceService.Domain.Enums;

namespace ComplianceService.Domain.Events;

/// <summary>
/// Raised when an OrganizationComplianceObligation status changes due to assessment,
/// journey completion, or manual update. Routed through the Outbox pipeline to
/// trigger snapshot recalculation and external relay.
/// </summary>
public class ObligationStatusUpdatedDomainEvent : IDomainEvent
{
    public Guid ObligationId { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid HospitalId { get; set; }

    public Guid FrameworkId { get; set; }

    public Guid ObjectiveElementId { get; set; }

    public ImplementationStatus ImplementationStatus { get; set; }

    public ObligationAssessmentStatus AssessmentStatus { get; set; }

    /// <summary>"Assessment" | "Journey" | "Manual"</summary>
    public string UpdateSource { get; set; } = string.Empty;

    public DateTime OccurredOn { get; set; }
}
