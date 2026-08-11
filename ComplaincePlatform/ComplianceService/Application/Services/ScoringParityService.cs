using ComplianceService.Application.Aggregation;
using ComplianceService.Application.ComplianceScoring;
using ComplianceService.Application.Contracts;
using ComplianceService.Domain.Enums;
using ComplianceService.Domain.Policies;
using ComplianceService.Infrastructure.Configs;
using Microsoft.Extensions.Options;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.Services;

public sealed class ScoringParityService : IScoringParityService
{
    private readonly IAggregationService _aggregation;
    private readonly IDeterministicNabhScoringService _deterministic;
    private readonly IOperationalModeClassificationPolicy _classificationPolicy;
    private readonly IOptions<MigrationSafetyOptions> _options;

    public ScoringParityService(
        IAggregationService aggregation,
        IDeterministicNabhScoringService deterministic,
        IOperationalModeClassificationPolicy classificationPolicy,
        IOptions<MigrationSafetyOptions> options)
    {
        _aggregation = aggregation;
        _deterministic = deterministic;
        _classificationPolicy = classificationPolicy;
        _options = options;
    }

    public async Task<ScoringParityResult> CompareAsync(
        Guid hospitalId,
        Guid frameworkId,
        Guid? journeyId,
        CancellationToken cancellationToken = default)
    {
        var deterministic = await _deterministic.EvaluateAsync(hospitalId, frameworkId, journeyId, cancellationToken);
        if (!deterministic.JourneyId.HasValue)
        {
            return new ScoringParityResult
            {
                HospitalId = hospitalId,
                FrameworkId = frameworkId,
                JourneyId = null,
                Category = "critical_mismatch",
                Mismatches = ["Missing journey context for parity comparison."]
            };
        }

        var included = _classificationPolicy.GetClassificationsForDecisionRules(OperationalMode.FinalAudit);
        var aggregate = await _aggregation.ComputeFrameworkAsync(frameworkId, deterministic.JourneyId.Value, included, cancellationToken);

        var mismatches = new List<string>();
        foreach (var newChapter in deterministic.Chapters)
        {
            var oldChapter = aggregate.Chapters.FirstOrDefault(c => c.ChapterId == newChapter.ChapterId);
            if (oldChapter is null)
            {
                mismatches.Add($"Missing chapter in old model: {newChapter.ChapterId}");
                continue;
            }

            if (Math.Abs(oldChapter.CompliancePercent - newChapter.CompliancePercent) > _options.Value.ScoreDeviationThresholdPercent)
            {
                mismatches.Add(
                    $"Chapter {newChapter.ChapterId} differs: old={oldChapter.CompliancePercent:F2}, new={newChapter.CompliancePercent:F2}");
            }

            foreach (var newStandard in newChapter.Standards)
            {
                var oldStandard = oldChapter.Standards.FirstOrDefault(s => s.ComplianceStandardId == newStandard.StandardId);
                if (oldStandard is null)
                {
                    mismatches.Add($"Missing standard in old model: {newStandard.StandardId}");
                    continue;
                }

                if (Math.Abs(oldStandard.CompliancePercent - newStandard.CompliancePercent) > _options.Value.ScoreDeviationThresholdPercent)
                {
                    mismatches.Add(
                        $"Standard {newStandard.StandardId} differs: old={oldStandard.CompliancePercent:F2}, new={newStandard.CompliancePercent:F2}");
                }
            }
        }

        var oldPass = aggregate.CompliancePercent >= 80m;
        var newPass = deterministic.Pass;
        if (oldPass != newPass)
        {
            mismatches.Add($"Pass/Fail mismatch: old={oldPass}, new={newPass}");
        }

        var delta = Math.Round(Math.Abs(aggregate.CompliancePercent - deterministic.CompliancePercent), 2);
        var category = mismatches.Count == 0
            ? "acceptable_deviation"
            : delta <= _options.Value.ScoreDeviationThresholdPercent
                ? "acceptable_deviation"
                : "critical_mismatch";

        return new ScoringParityResult
        {
            HospitalId = hospitalId,
            FrameworkId = frameworkId,
            JourneyId = deterministic.JourneyId,
            OldCompliancePercent = aggregate.CompliancePercent,
            NewCompliancePercent = deterministic.CompliancePercent,
            Delta = delta,
            OldPass = oldPass,
            NewPass = newPass,
            Category = category,
            Mismatches = mismatches
        };
    }
}
