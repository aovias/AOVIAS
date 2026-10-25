using ComplianceService.Application.Commands;
using ComplianceService.Application.Contracts;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Domain.Events;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ComplianceService.Application.Services;

/// <summary>
/// Centralized projection pipeline for <see cref="OrganizationComplianceObligation"/>.
/// Recomputes operational state from assessment results, journey progress, evidence links,
/// and task completion — then evaluates deficiencies and publishes outbox events.
/// </summary>
public sealed class ObligationProjectionService : IObligationProjectionService
{
    private readonly ComplianceDbContext _db;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly EvaluateObligationDeficienciesCommandHandler _evaluateDeficienciesHandler;
    private readonly IStageUnlockingService _stageUnlocking;
    private readonly ILogger<ObligationProjectionService> _logger;

    public ObligationProjectionService(
        ComplianceDbContext db,
        IDomainEventDispatcher eventDispatcher,
        IUnitOfWork unitOfWork,
        EvaluateObligationDeficienciesCommandHandler evaluateDeficienciesHandler,
        IStageUnlockingService stageUnlocking,
        ILogger<ObligationProjectionService> logger)
    {
        _db = db;
        _eventDispatcher = eventDispatcher;
        _unitOfWork = unitOfWork;
        _evaluateDeficienciesHandler = evaluateDeficienciesHandler;
        _stageUnlocking = stageUnlocking;
        _logger = logger;
    }

    public Task<ObligationProjectionResult?> ProjectByObjectiveElementAsync(
        Guid hospitalId,
        Guid objectiveElementId,
        string triggerSource,
        CancellationToken cancellationToken = default)
    {
        if (hospitalId == Guid.Empty || objectiveElementId == Guid.Empty)
        {
            return Task.FromResult<ObligationProjectionResult?>(null);
        }

        return ProjectByObjectiveElementInternalAsync(hospitalId, objectiveElementId, triggerSource, cancellationToken);
    }

    public async Task<ObligationProjectionResult?> ProjectAsync(
        ObligationProjectionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.ObligationId == Guid.Empty)
        {
            return null;
        }

        var obligation = await _db.OrganizationComplianceObligations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.Id == request.ObligationId, cancellationToken);

        if (obligation == null)
        {
            _logger.LogWarning(
                "ObligationProjection: Obligation {ObligationId} not found (trigger={Trigger}).",
                request.ObligationId,
                request.TriggerSource);
            return null;
        }

        return await ProjectCoreAsync(obligation, request, cancellationToken);
    }

    private async Task<ObligationProjectionResult?> ProjectByObjectiveElementInternalAsync(
        Guid hospitalId,
        Guid objectiveElementId,
        string triggerSource,
        CancellationToken cancellationToken)
    {
        var obligation = await _db.OrganizationComplianceObligations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                o => o.HospitalId == hospitalId && o.ObjectiveElementId == objectiveElementId,
                cancellationToken);

        if (obligation == null)
        {
            _logger.LogDebug(
                "ObligationProjection: No obligation for HospitalId={HospitalId} OeId={OeId} (trigger={Trigger}).",
                hospitalId,
                objectiveElementId,
                triggerSource);
            return null;
        }

        return await ProjectCoreAsync(
            obligation,
            new ObligationProjectionRequest(obligation.Id, triggerSource),
            cancellationToken);
    }

    private async Task<ObligationProjectionResult?> ProjectCoreAsync(
        OrganizationComplianceObligation obligation,
        ObligationProjectionRequest request,
        CancellationToken cancellationToken)
    {
        var evidenceLinks = await _db.ObjectiveElementEvidences
            .AsNoTracking()
            .Where(l => l.ObjectiveElementId == obligation.ObjectiveElementId)
            .ToListAsync(cancellationToken);

        var objectiveJourney = await (
            from oj in _db.ObjectiveJourneys.AsNoTracking()
            join j in _db.ComplianceJourneys.AsNoTracking() on oj.ComplianceJourneyId equals j.Id
            where j.HospitalId == obligation.HospitalId
                  && j.FrameworkId == obligation.FrameworkId
                  && oj.ObjectiveElementId == obligation.ObjectiveElementId
            orderby oj.LastUpdatedAt descending
            select oj)
            .FirstOrDefaultAsync(cancellationToken);

        var hasCompletedTask = await _db.ComplianceTasks
            .AsNoTracking()
            .AnyAsync(
                t => t.HospitalId == obligation.HospitalId
                     && t.ObjectiveElementId == obligation.ObjectiveElementId
                     && t.Status == ComplianceTaskStatus.Completed,
                cancellationToken);

        var evidenceStatus = ObligationProjectionRules.ComputeEvidenceStatus(evidenceLinks);
        var verificationStatus = ObligationProjectionRules.ComputeVerificationStatus(evidenceLinks);
        var (journeyStatus, completionPercentage) =
            ObligationProjectionRules.ComputeJourneyProgress(objectiveJourney);

        obligation.JourneyStatus = journeyStatus;
        obligation.EvidenceStatus = evidenceStatus;
        obligation.VerificationStatus = verificationStatus;
        obligation.CompletionPercentage = completionPercentage;
        obligation.ImplementationStatus = ObligationProjectionRules.ComputeImplementationStatus(
            obligation.ImplementationStatus,
            objectiveJourney,
            hasCompletedTask);

        obligation.ReadinessScore = ObligationReadinessCalculator.CalculateObligationReadiness(
            obligation.AssessmentStatus,
            obligation.ImplementationStatus,
            obligation.EvidenceStatus,
            obligation.VerificationStatus);

        var now = DateTime.UtcNow;
        obligation.UpdatedAt = now;
        obligation.LastSnapshotRefreshDate = now;
        obligation.UpdatedBy = $"projection:{request.TriggerSource}";

        if (evidenceLinks.Count > 0)
        {
            obligation.LastEvidenceDate = evidenceLinks.Max(l => l.LinkedAt);
        }
        else if (evidenceStatus == EvidenceStatus.Missing)
        {
            obligation.LastEvidenceDate = null;
        }

        var latestVerification = evidenceLinks
            .Where(l => l.VerifiedAt.HasValue)
            .OrderByDescending(l => l.VerifiedAt)
            .FirstOrDefault();

        if (latestVerification?.VerifiedAt != null)
        {
            obligation.LastVerificationDate = latestVerification.VerifiedAt;
        }
        else if (verificationStatus == VerificationStatus.NotVerified)
        {
            obligation.LastVerificationDate = null;
        }

        var domainEvent = new ObligationStatusUpdatedDomainEvent
        {
            ObligationId = obligation.Id,
            OrganizationId = obligation.OrganizationId,
            HospitalId = obligation.HospitalId,
            FrameworkId = obligation.FrameworkId,
            ObjectiveElementId = obligation.ObjectiveElementId,
            ImplementationStatus = obligation.ImplementationStatus,
            AssessmentStatus = obligation.AssessmentStatus,
            UpdateSource = request.TriggerSource,
            OccurredOn = now
        };

        await _eventDispatcher.DispatchAsync(new[] { domainEvent }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "ObligationProjection: Updated obligation {ObligationId} trigger={Trigger} " +
            "Evidence={Evidence} Verification={Verification} Readiness={Readiness:F1} Completion={Completion:F0}%",
            obligation.Id,
            request.TriggerSource,
            evidenceStatus,
            verificationStatus,
            obligation.ReadinessScore,
            obligation.CompletionPercentage);

        if (request.EvaluateDeficiencies)
        {
            await _evaluateDeficienciesHandler.HandleAsync(
                new EvaluateObligationDeficienciesCommand(
                    obligation.Id,
                    obligation.HospitalId,
                    obligation.FrameworkId,
                    request.TriggerSource,
                    FlushOutbox: request.FlushOutbox),
                cancellationToken);
        }
        else if (request.FlushOutbox)
        {
            await _evaluateDeficienciesHandler.FlushOutboxAsync(cancellationToken);
        }

        await TriggerStageUnlockEvaluationAsync(obligation, request.TriggerSource, cancellationToken);

        return new ObligationProjectionResult(
            obligation.Id,
            obligation.HospitalId,
            obligation.FrameworkId,
            obligation.ObjectiveElementId,
            evidenceStatus.ToString(),
            verificationStatus.ToString(),
            obligation.ReadinessScore,
            obligation.CompletionPercentage);
    }

    private async Task TriggerStageUnlockEvaluationAsync(
        OrganizationComplianceObligation obligation,
        string triggerSource,
        CancellationToken cancellationToken)
    {
        try
        {
            var journeyId = await _db.ComplianceJourneys
                .AsNoTracking()
                .Where(j => j.HospitalId == obligation.HospitalId && j.FrameworkId == obligation.FrameworkId)
                .OrderByDescending(j => j.EnrollmentDate)
                .Select(j => j.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (journeyId == Guid.Empty)
            {
                return;
            }

            await _stageUnlocking.EvaluateAndPersistAsync(
                journeyId,
                $"obligation-projection:{triggerSource}",
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Stage unlock evaluation failed (non-fatal) after obligation projection for {ObligationId}",
                obligation.Id);
        }
    }
}
