using System.Text.Json;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.Events;
using ComplianceService.Application.StageUnlock;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Application.Services;

/// <summary>
/// Evaluates earned compliance progression and unlocks maturity stages sequentially.
/// </summary>
public sealed class StageUnlockingService : IStageUnlockingService
{
    private static readonly ObjectiveElementClassification[] StageOrder =
    [
        ObjectiveElementClassification.Core,
        ObjectiveElementClassification.Commitment,
        ObjectiveElementClassification.Achievement,
        ObjectiveElementClassification.Excellence
    ];

    private readonly ComplianceDbContext _db;
    private readonly IComplianceEventEmitter _eventEmitter;
    private readonly IComplianceSnapshotRecalculationQueue _snapshotQueue;
    private readonly ILogger<StageUnlockingService> _logger;

    public StageUnlockingService(
        ComplianceDbContext db,
        IComplianceEventEmitter eventEmitter,
        IComplianceSnapshotRecalculationQueue snapshotQueue,
        ILogger<StageUnlockingService> logger)
    {
        _db = db;
        _eventEmitter = eventEmitter;
        _snapshotQueue = snapshotQueue;
        _logger = logger;
    }

    public async Task<IReadOnlyList<StageUnlockEvaluationResult>> EvaluateAndPersistAsync(
        Guid journeyId,
        string triggerSource,
        Guid? triggeredByUserId = null,
        CancellationToken cancellationToken = default)
    {
        if (journeyId == Guid.Empty)
        {
            return Array.Empty<StageUnlockEvaluationResult>();
        }

        var journey = await _db.ComplianceJourneys
            .AsNoTracking()
            .Where(j => j.Id == journeyId)
            .Select(j => new { j.Id, j.HospitalId, j.OrganizationId, j.FrameworkId })
            .FirstOrDefaultAsync(cancellationToken);

        if (journey == null)
        {
            return Array.Empty<StageUnlockEvaluationResult>();
        }

        var evaluations = await EvaluateInternalAsync(
            journey.Id,
            journey.HospitalId,
            journey.OrganizationId,
            journey.FrameworkId,
            cancellationToken);

        var existing = await _db.OrganizationMaturityStageStates
            .Where(s => s.JourneyId == journeyId)
            .ToDictionaryAsync(s => s.Stage, cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var eval in evaluations)
        {
            if (!existing.TryGetValue(eval.Stage, out var row))
            {
                row = new OrganizationMaturityStageState
                {
                    Id = Guid.NewGuid(),
                    JourneyId = journeyId,
                    HospitalId = journey.HospitalId,
                    OrganizationId = journey.OrganizationId,
                    FrameworkId = journey.FrameworkId,
                    Stage = eval.Stage,
                    CreatedAt = now
                };
                _db.OrganizationMaturityStageStates.Add(row);
                existing[eval.Stage] = row;
            }

            var previous = row.State;
            row.State = eval.State;
            row.ProgressPercent = eval.ProgressPercent;
            row.ReadinessPercent = eval.ReadinessPercent;
            row.UnlockReadinessPercent = eval.UnlockReadinessPercent;
            row.LastEvaluatedAt = now;
            row.BlockingReasonsJson = Serialize(eval.BlockingReasons);
            row.RecommendationsJson = Serialize(eval.Recommendations);

            if (eval.State != MaturityStageLifecycleState.Locked && row.UnlockedAt == null
                && eval.Stage != ObjectiveElementClassification.Core)
            {
                row.UnlockedAt = now;
            }

            if (eval.CriteriaMet && row.CompletedAt == null)
            {
                row.CompletedAt = now;
            }
            else if (!eval.CriteriaMet)
            {
                row.CompletedAt = null;
            }

            if (eval.StateChanged)
            {
                _db.MaturityStageUnlockAuditEntries.Add(new MaturityStageUnlockAuditEntry
                {
                    Id = Guid.NewGuid(),
                    JourneyId = journeyId,
                    HospitalId = journey.HospitalId,
                    FrameworkId = journey.FrameworkId,
                    Stage = eval.Stage,
                    PreviousState = previous,
                    NewState = eval.State,
                    TriggerSource = triggerSource,
                    TriggeredByUserId = triggeredByUserId,
                    EvaluatedAt = now,
                    CriteriaPassedJson = Serialize(eval.Criteria.Where(c => c.Passed).Select(c => c.Label)),
                    CriteriaFailedJson = Serialize(eval.Criteria.Where(c => !c.Passed).Select(c => c.Label)),
                    CreatedAt = now
                });

                await EmitStateChangeEventsAsync(
                    journey.HospitalId,
                    journey.OrganizationId,
                    eval,
                    previous,
                    triggeredByUserId,
                    cancellationToken);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        _snapshotQueue.EnqueueMaturityRecalc(journey.HospitalId, journey.OrganizationId);
        _snapshotQueue.EnqueueHospitalFramework(journey.HospitalId, journey.FrameworkId);

        _logger.LogInformation(
            "StageUnlock: Evaluated journey {JourneyId} trigger={Trigger} stages={Count}",
            journeyId,
            triggerSource,
            evaluations.Count);

        return evaluations;
    }

    public async Task<IReadOnlyList<StageUnlockEvaluationResult>> GetStageEvaluationsAsync(
        Guid hospitalId,
        Guid organizationId,
        Guid frameworkId,
        CancellationToken cancellationToken = default)
    {
        var journeyId = await ResolveJourneyIdAsync(hospitalId, organizationId, cancellationToken);
        if (journeyId == Guid.Empty)
        {
            return Array.Empty<StageUnlockEvaluationResult>();
        }

        return await EvaluateInternalAsync(journeyId, hospitalId, organizationId, frameworkId, cancellationToken);
    }

    public async Task<bool> IsStageAccessibleAsync(
        Guid journeyId,
        ObjectiveElementClassification stage,
        CancellationToken cancellationToken = default)
    {
        if (stage == ObjectiveElementClassification.Core)
        {
            return true;
        }

        var row = await _db.OrganizationMaturityStageStates
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.JourneyId == journeyId && s.Stage == stage, cancellationToken);

        if (row != null)
        {
            return MaturityStageStateMachine.IsAccessible(row.State);
        }

        var eval = await GetStageEvaluationAsync(journeyId, stage, cancellationToken);
        return eval != null && MaturityStageStateMachine.IsAccessible(eval.State);
    }

    public async Task<StageUnlockEvaluationResult?> GetStageEvaluationAsync(
        Guid journeyId,
        ObjectiveElementClassification stage,
        CancellationToken cancellationToken = default)
    {
        var journey = await _db.ComplianceJourneys
            .AsNoTracking()
            .Where(j => j.Id == journeyId)
            .Select(j => new { j.HospitalId, j.OrganizationId, j.FrameworkId })
            .FirstOrDefaultAsync(cancellationToken);

        if (journey == null)
        {
            return null;
        }

        var all = await EvaluateInternalAsync(
            journeyId,
            journey.HospitalId,
            journey.OrganizationId,
            journey.FrameworkId,
            cancellationToken);

        return all.FirstOrDefault(e => e.Stage == stage);
    }

    private async Task<IReadOnlyList<StageUnlockEvaluationResult>> EvaluateInternalAsync(
        Guid journeyId,
        Guid hospitalId,
        Guid organizationId,
        Guid frameworkId,
        CancellationToken cancellationToken)
    {
        var obligations = await _db.OrganizationComplianceObligations
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(o => o.HospitalId == hospitalId && o.FrameworkId == frameworkId)
            .ToListAsync(cancellationToken);

        var criticalByStage = await LoadCriticalDeficiencyCountsByStageAsync(
            hospitalId,
            frameworkId,
            cancellationToken);

        var persisted = await _db.OrganizationMaturityStageStates
            .AsNoTracking()
            .Where(s => s.JourneyId == journeyId)
            .ToDictionaryAsync(s => s.Stage, s => s.State, cancellationToken);

        var results = new List<StageUnlockEvaluationResult>();
        var prerequisiteComplete = true;

        foreach (var stage in StageOrder)
        {
            var stageObs = obligations.Where(o => o.MaturityLevel == stage).ToList();
            criticalByStage.TryGetValue(stage, out var criticalCount);

            var metrics = StageUnlockCriteriaEvaluator.BuildMetrics(stage, stageObs, criticalCount);
            var criteria = StageUnlockCriteriaRegistry.Get(stage);
            var previous = persisted.TryGetValue(stage, out var ps)
                ? ps
                : stage == ObjectiveElementClassification.Core
                    ? MaturityStageLifecycleState.Available
                    : MaturityStageLifecycleState.Locked;

            var eval = StageUnlockCriteriaEvaluator.Evaluate(
                stage,
                metrics,
                criteria,
                previous,
                prerequisiteComplete);

            results.Add(eval);
            prerequisiteComplete = eval.CriteriaMet;
        }

        return results;
    }

    private async Task<Dictionary<ObjectiveElementClassification, int>> LoadCriticalDeficiencyCountsByStageAsync(
        Guid hospitalId,
        Guid frameworkId,
        CancellationToken cancellationToken)
    {
        var rows = await (
            from d in _db.ComplianceDeficiencies.AsNoTracking().IgnoreQueryFilters()
            join o in _db.OrganizationComplianceObligations.AsNoTracking().IgnoreQueryFilters()
                on d.ObjectiveElementId equals o.ObjectiveElementId
            where d.HospitalId == hospitalId
                  && d.FrameworkId == frameworkId
                  && o.HospitalId == hospitalId
                  && o.FrameworkId == frameworkId
                  && d.Status != DeficiencyStatus.Closed
                  && d.Severity == DeficiencySeverity.Critical
            select o.MaturityLevel).ToListAsync(cancellationToken);

        return rows
            .GroupBy(x => x)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private async Task<Guid> ResolveJourneyIdAsync(
        Guid hospitalId,
        Guid organizationId,
        CancellationToken cancellationToken) =>
        await _db.ComplianceJourneys
            .AsNoTracking()
            .Where(j => j.HospitalId == hospitalId && j.OrganizationId == organizationId)
            .OrderByDescending(j => j.EnrollmentDate)
            .Select(j => j.Id)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task EmitStateChangeEventsAsync(
        Guid hospitalId,
        Guid organizationId,
        StageUnlockEvaluationResult eval,
        MaturityStageLifecycleState previous,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        if (eval.State == MaturityStageLifecycleState.Completed && previous != MaturityStageLifecycleState.Completed)
        {
            await _eventEmitter.EmitAsync(
                ComplianceEventType.StageCompleted,
                hospitalId,
                nameof(OrganizationMaturityStageState),
                hospitalId,
                userId,
                new { eval.Stage, organizationId },
                cancellationToken);

            var next = MaturityStageStateMachine.NextStage(eval.Stage);
            if (next.HasValue)
            {
                await _eventEmitter.EmitAsync(
                    ComplianceEventType.StageUnlocked,
                    hospitalId,
                    nameof(OrganizationMaturityStageState),
                    hospitalId,
                    userId,
                    new { Stage = next.Value, UnlockedBy = eval.Stage, organizationId },
                    cancellationToken);
            }
        }
        else if (eval.State == MaturityStageLifecycleState.Available
                 && previous == MaturityStageLifecycleState.Locked)
        {
            await _eventEmitter.EmitAsync(
                ComplianceEventType.StageUnlocked,
                hospitalId,
                nameof(OrganizationMaturityStageState),
                hospitalId,
                userId,
                new { eval.Stage, organizationId },
                cancellationToken);
        }

        if (eval.BlockingReasons.Any(r => r.Contains("Critical", StringComparison.OrdinalIgnoreCase)))
        {
            await _eventEmitter.EmitAsync(
                ComplianceEventType.CriticalDeficiencyBlockingProgress,
                hospitalId,
                nameof(ComplianceDeficiency),
                hospitalId,
                userId,
                new { eval.Stage, eval.BlockingReasons },
                cancellationToken);
        }

        if (eval.CriteriaMet)
        {
            await _eventEmitter.EmitAsync(
                ComplianceEventType.ReadinessThresholdAchieved,
                hospitalId,
                nameof(OrganizationMaturityStageState),
                hospitalId,
                userId,
                new { eval.Stage, eval.ReadinessPercent },
                cancellationToken);
        }
    }

    private static string? Serialize(IEnumerable<string> values)
    {
        var list = values.ToList();
        return list.Count == 0 ? null : JsonSerializer.Serialize(list);
    }
}
