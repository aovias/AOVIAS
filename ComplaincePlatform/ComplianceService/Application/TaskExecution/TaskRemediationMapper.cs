using ComplianceService.Application.DTOs;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;

namespace ComplianceService.Application.TaskExecution;

internal static class TaskRemediationMapper
{
    public static TaskRelatedDeficiencyDto? MapDeficiency(ComplianceDeficiency? deficiency) =>
        deficiency == null
            ? null
            : new TaskRelatedDeficiencyDto
            {
                Id = deficiency.Id,
                Title = deficiency.Title,
                Description = deficiency.Description,
                DeficiencyType = deficiency.DeficiencyType.ToString(),
                Severity = deficiency.Severity.ToString(),
                Status = deficiency.Status.ToString()
            };

    public static TaskRelatedObligationDto? MapObligation(
        OrganizationComplianceObligation? obligation,
        string? nextActionLabel) =>
        obligation == null
            ? null
            : new TaskRelatedObligationDto
            {
                Id = obligation.Id,
                EvidenceStatus = obligation.EvidenceStatus.ToString(),
                VerificationStatus = obligation.VerificationStatus.ToString(),
                AssessmentStatus = obligation.AssessmentStatus.ToString(),
                ImplementationStatus = obligation.ImplementationStatus.ToString(),
                ReadinessScore = obligation.ReadinessScore,
                NextActionLabel = nextActionLabel
            };

    public static string? MapMaturityStageLabel(ObjectiveElementClassification? stage) =>
        stage switch
        {
            ObjectiveElementClassification.Core => "Core",
            ObjectiveElementClassification.Commitment => "Commitment",
            ObjectiveElementClassification.Achievement => "Achievement",
            ObjectiveElementClassification.Excellence => "Excellence",
            _ => null
        };
}
