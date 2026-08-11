using ComplianceService.Domain.Enums;

namespace ComplianceService.Application.Commands;

/// <summary>
/// Updates journey progress and evidence status on an OrganizationComplianceObligation.
/// Raised when an ObjectiveJourney wizard step is saved, started, or completed.
/// </summary>
public class UpdateObligationJourneyProgressCommand
{
    public Guid OrganizationId { get; set; }
    public Guid ObjectiveElementId { get; set; }

    public ObligationJourneyStatus JourneyStatus { get; set; }
    public EvidenceStatus EvidenceStatus { get; set; }
    public decimal CompletionPercentage { get; set; }

    public string? UpdatedBy { get; set; }

    public UpdateObligationJourneyProgressCommand(
        Guid organizationId,
        Guid objectiveElementId,
        ObligationJourneyStatus journeyStatus,
        EvidenceStatus evidenceStatus,
        decimal completionPercentage,
        string? updatedBy = null)
    {
        OrganizationId = organizationId;
        ObjectiveElementId = objectiveElementId;
        JourneyStatus = journeyStatus;
        EvidenceStatus = evidenceStatus;
        CompletionPercentage = completionPercentage;
        UpdatedBy = updatedBy;
    }
}
