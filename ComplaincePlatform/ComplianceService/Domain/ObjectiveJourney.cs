using ComplianceService.Domain.Enums;

namespace ComplianceService.Domain;

/// <summary>
/// Per (compliance journey × objective element) wizard execution state.
/// Identity: <see cref="ComplianceJourneyId"/> + <see cref="ObjectiveElementId"/> (unique).
/// </summary>
public class ObjectiveJourney
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid HospitalId { get; set; }

    public Guid ComplianceJourneyId { get; set; }

    public Guid ObjectiveElementId { get; set; }

    /// <summary>Full wizard JSON (canonical form validated on write).</summary>
    public string WizardPayloadJson { get; set; } = "{}";

    public int CurrentStepIndex { get; set; }

    public ObjectiveJourneyStatus Status { get; set; }

    public DateTime LastUpdatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
