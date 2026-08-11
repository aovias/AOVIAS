using ComplianceService.Domain;

namespace ComplianceService.Application.Scoring
{
    public interface IScoringService
    {
        Task<ObjectiveElementScore> SetScoreAsync(
            Guid objectiveElementId,
            Guid journeyId,
            Guid assessorId,
            int score,
            string rationale,
            decimal evidenceSampleCompliancePercent,
            bool isDocumentationInadequate,
            string? downgradeReason,
            CancellationToken cancellationToken = default);

        Task<ObjectiveElementScore> SetNAAsync(
            Guid objectiveElementId,
            Guid journeyId,
            bool isApplicable,
            string? naReason,
            Guid? naApprovedBy,
            CancellationToken cancellationToken = default);

        Task<ObjectiveElementEvidence> LinkEvidenceAsync(
            Guid objectiveElementId,
            Guid evidenceId,
            Guid linkedBy,
            CancellationToken cancellationToken = default);

        Task UnlinkEvidenceAsync(
            Guid objectiveElementEvidenceId,
            CancellationToken cancellationToken = default);
    }
}

