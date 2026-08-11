using System.Globalization;
using ComplianceService.Application.AI.Contracts;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Application.Services;
using ComplianceService.Application.StageUnlock;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Application.AI.Providers;

public sealed class AIMaturityContextProvider : IAIMaturityContextProvider
{
    private readonly IStageUnlockingService _stageUnlocking;
    private readonly ComplianceMaturityIndexService _cmiService;
    private readonly ComplianceDbContext _db;

    public AIMaturityContextProvider(
        IStageUnlockingService stageUnlocking,
        ComplianceMaturityIndexService cmiService,
        ComplianceDbContext db)
    {
        _stageUnlocking = stageUnlocking;
        _cmiService = cmiService;
        _db = db;
    }

    public async Task<AIContextContribution> BuildAsync(AIContextBuildContext context, CancellationToken cancellationToken = default)
    {
        var scope = context.Scope;
        var facts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var sources = new List<AIContextSourceDto>();

        var journey = await _db.ComplianceJourneys.AsNoTracking()
            .Where(j => j.HospitalId == scope.HospitalId
                        && j.OrganizationId == scope.OrganizationId
                        && j.FrameworkId == scope.FrameworkId)
            .OrderByDescending(j => j.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (journey == null)
        {
            facts["maturityStatus"] = "No active compliance journey found for this framework.";
            return new AIContextContribution { Facts = facts, Sources = sources };
        }

        var evaluations = await _stageUnlocking.GetStageEvaluationsAsync(
            scope.HospitalId, scope.OrganizationId, scope.FrameworkId, cancellationToken);

        var core = evaluations.FirstOrDefault(e => e.Stage == ObjectiveElementClassification.Core);
        var commitment = evaluations.FirstOrDefault(e => e.Stage == ObjectiveElementClassification.Commitment);
        var achievement = evaluations.FirstOrDefault(e => e.Stage == ObjectiveElementClassification.Achievement);
        var excellence = evaluations.FirstOrDefault(e => e.Stage == ObjectiveElementClassification.Excellence);

        var cmi = _cmiService.Calculate(
            core?.ReadinessPercent ?? 0,
            commitment?.ReadinessPercent ?? 0,
            achievement?.ReadinessPercent ?? 0,
            excellence?.ReadinessPercent ?? 0);

        facts["complianceMaturityIndex"] = cmi.Value.ToString(CultureInfo.InvariantCulture);
        facts["maturityCoreProgress"] = Math.Round(core?.ProgressPercent ?? 0, 0).ToString(CultureInfo.InvariantCulture);
        facts["maturityCommitmentProgress"] = Math.Round(commitment?.ProgressPercent ?? 0, 0).ToString(CultureInfo.InvariantCulture);
        facts["maturityAchievementProgress"] = Math.Round(achievement?.ProgressPercent ?? 0, 0).ToString(CultureInfo.InvariantCulture);

        var currentStage = ResolveCurrentStageLabel(evaluations);
        facts["maturityCurrentStage"] = currentStage;

        var lockedStages = evaluations
            .Where(e => e.State == MaturityStageLifecycleState.Locked)
            .Select(e => e.Stage.ToString())
            .ToList();
        if (lockedStages.Count > 0)
        {
            facts["maturityLockedStages"] = string.Join(", ", lockedStages);
        }

        var nextLocked = evaluations
            .Where(e => e.State == MaturityStageLifecycleState.Locked)
            .OrderBy(e => (int)e.Stage)
            .FirstOrDefault();

        if (nextLocked != null)
        {
            facts["maturityNextUnlockTarget"] = nextLocked.Stage.ToString();
            facts["maturityUnlockReadinessPercent"] = Math.Round(nextLocked.UnlockReadinessPercent, 0)
                .ToString(CultureInfo.InvariantCulture);

            if (nextLocked.BlockingReasons.Count > 0)
            {
                facts["maturityBlockingReasons"] = string.Join("; ", nextLocked.BlockingReasons.Take(5));
            }

            if (nextLocked.Recommendations.Count > 0)
            {
                facts["maturityNextStageRequirements"] = string.Join("; ", nextLocked.Recommendations.Take(5));
            }

            sources.Add(new AIContextSourceDto
            {
                SourceType = "StageUnlock",
                SourceId = nextLocked.Stage.ToString(),
                Summary = $"{nextLocked.Stage} is locked — {string.Join("; ", nextLocked.BlockingReasons.Take(2))}",
                RelevanceScore = 0.96m
            });
        }

        foreach (var eval in evaluations.Where(e => e.BlockingReasons.Count > 0).Take(3))
        {
            sources.Add(new AIContextSourceDto
            {
                SourceType = "StageBlocker",
                SourceId = eval.Stage.ToString(),
                Summary = $"{eval.Stage}: {string.Join("; ", eval.BlockingReasons.Take(2))}",
                RelevanceScore = eval.State == MaturityStageLifecycleState.Locked ? 0.92m : 0.7m
            });
        }

        return new AIContextContribution { Facts = facts, Sources = sources };
    }

    private static string ResolveCurrentStageLabel(IReadOnlyList<StageUnlockEvaluationResult> evaluations)
    {
        var active = evaluations
            .Where(e => e.State is MaturityStageLifecycleState.InProgress or MaturityStageLifecycleState.Available or MaturityStageLifecycleState.Completed)
            .OrderByDescending(e => (int)e.Stage)
            .FirstOrDefault();

        return active?.Stage.ToString() ?? "Core";
    }
}
