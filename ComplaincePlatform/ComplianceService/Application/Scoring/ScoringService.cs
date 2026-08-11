using ComplianceService.Application.Contracts;
using ComplianceService.Domain;
using ComplianceService.Domain.Events;
using ComplianceService.Infrastructure.Configs;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ComplianceService.Application.Scoring
{
    public class ScoringService : IScoringService
    {
        private readonly ComplianceDbContext _db;
        private readonly IEvidenceRegistrationService _evidenceRegistration;
        private readonly ScoringOptions _options;
        private readonly ActionPlans.IActionPlanService _actionPlanService;

        public ScoringService(
            ComplianceDbContext db,
            IEvidenceRegistrationService evidenceRegistration,
            IOptions<ScoringOptions> options,
            ActionPlans.IActionPlanService actionPlanService)
        {
            _db = db;
            _evidenceRegistration = evidenceRegistration;
            _options = options.Value;
            _actionPlanService = actionPlanService;
        }

        public async Task<ObjectiveElementScore> SetScoreAsync(
            Guid objectiveElementId,
            Guid journeyId,
            Guid assessorId,
            int score,
            string rationale,
            decimal evidenceSampleCompliancePercent,
            bool isDocumentationInadequate,
            string? downgradeReason,
            CancellationToken cancellationToken = default)
        {
            if (score < 1 || score > 5)
            {
                throw new ArgumentOutOfRangeException(nameof(score), "Score must be between 1 and 5.");
            }

            if (score <= _options.RequireRationaleThreshold && string.IsNullOrWhiteSpace(rationale))
            {
                throw new InvalidOperationException("Rationale is required for scores at or below the configured threshold.");
            }

            var oe = await _db.ObjectiveElements
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == objectiveElementId, cancellationToken)
                ?? throw new InvalidOperationException("Objective Element not found.");

            // If currently NA for this journey, prevent scoring until cleared
            var latest = await _db.ObjectiveElementScores
                .Where(s => s.ObjectiveElementId == objectiveElementId && s.JourneyId == journeyId)
                .OrderByDescending(s => s.AssessedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (latest != null && latest.IsApplicable == false)
            {
                throw new InvalidOperationException("Cannot score an Objective Element marked as Not Applicable. Clear NA first.");
            }

            var appliedScore = score;
            var downgradeApplied = false;

            if (_options.DowngradeEnabled && isDocumentationInadequate)
            {
                appliedScore = Math.Max(1, score - Math.Abs(_options.DowngradeStep));
                downgradeApplied = appliedScore != score;
            }

            var entry = new ObjectiveElementScore
            {
                Id = Guid.NewGuid(),
                ObjectiveElementId = objectiveElementId,
                JourneyId = journeyId,
                AssessorId = assessorId,
                Score = appliedScore,
                Rationale = rationale,
                EvidenceSampleCompliancePercent = evidenceSampleCompliancePercent,
                IsDocumentationInadequate = isDocumentationInadequate,
                DowngradeApplied = downgradeApplied,
                DowngradeReason = downgradeApplied ? (downgradeReason ?? "Documentation inadequate") : null,
                IsApplicable = true,
                NaReason = null,
                NaApprovedBy = null,
                AssessedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _db.ObjectiveElementScores.Add(entry);
            await _db.SaveChangesAsync(cancellationToken);

            // Raise domain event (to be dispatched via outbox pattern elsewhere)
            _ = new ObjectiveElementScoredDomainEvent(objectiveElementId, journeyId, assessorId, appliedScore, entry.AssessedAt);

            // Trigger action plan creation for low scores (<=3), idempotent
            await _actionPlanService.EnsureForLowScoreAsync(
                objectiveElementId,
                journeyId,
                appliedScore,
                assessorId,
                defaultRootCause: null,
                defaultCorrectiveAction: null,
                dueDate: null,
                cancellationToken);

            return entry;
        }

        public async Task<ObjectiveElementScore> SetNAAsync(
            Guid objectiveElementId,
            Guid journeyId,
            bool isApplicable,
            string? naReason,
            Guid? naApprovedBy,
            CancellationToken cancellationToken = default)
        {
            if (!isApplicable)
            {
                if (string.IsNullOrWhiteSpace(naReason))
                {
                    throw new InvalidOperationException("NA reason is required when marking as Not Applicable.");
                }
                if (_options.RequireNaApproval && !naApprovedBy.HasValue)
                {
                    throw new InvalidOperationException("NA approval is required by policy.");
                }
            }

            var entry = new ObjectiveElementScore
            {
                Id = Guid.NewGuid(),
                ObjectiveElementId = objectiveElementId,
                JourneyId = journeyId,
                AssessorId = Guid.Empty, // system set
                Score = 0, // score not counted when NA; stored as 0 sentinel
                Rationale = isApplicable ? "Applicability restored" : "Marked Not Applicable",
                EvidenceSampleCompliancePercent = 0,
                IsDocumentationInadequate = false,
                DowngradeApplied = false,
                DowngradeReason = null,
                IsApplicable = isApplicable,
                NaReason = isApplicable ? null : naReason,
                NaApprovedBy = isApplicable ? null : naApprovedBy,
                AssessedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _db.ObjectiveElementScores.Add(entry);
            await _db.SaveChangesAsync(cancellationToken);

            _ = new ObjectiveElementNAChangedDomainEvent(objectiveElementId, journeyId, isApplicable, naReason, naApprovedBy, entry.AssessedAt);

            return entry;
        }

        public async Task<ObjectiveElementEvidence> LinkEvidenceAsync(
            Guid objectiveElementId,
            Guid evidenceId,
            Guid linkedBy,
            CancellationToken cancellationToken = default)
        {
            await _evidenceRegistration.LinkExistingEvidenceAsync(
                new EvidenceRegistrationLinkRequest
                {
                    EvidenceId = evidenceId,
                    ObjectiveElementId = objectiveElementId,
                    LinkedByUserId = linkedBy
                },
                cancellationToken);

            return await _db.ObjectiveElementEvidences
                .FirstAsync(
                    x => x.ObjectiveElementId == objectiveElementId && x.EvidenceId == evidenceId,
                    cancellationToken);
        }

        public async Task UnlinkEvidenceAsync(
            Guid objectiveElementEvidenceId,
            CancellationToken cancellationToken = default)
        {
            var link = await _db.ObjectiveElementEvidences
                .FirstOrDefaultAsync(x => x.Id == objectiveElementEvidenceId, cancellationToken)
                ?? throw new InvalidOperationException("ObjectiveElementEvidence link not found.");

            _db.ObjectiveElementEvidences.Remove(link);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}

