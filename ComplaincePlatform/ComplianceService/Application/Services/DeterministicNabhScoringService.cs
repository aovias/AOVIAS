using ComplianceService.Application.ComplianceScoring;
using ComplianceService.Application.Contracts;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Application.Services;

public sealed class DeterministicNabhScoringService : IDeterministicNabhScoringService
{
    private readonly ComplianceDbContext _db;

    public DeterministicNabhScoringService(ComplianceDbContext db)
    {
        _db = db;
    }

    public async Task<DeterministicNabhScoreResult> EvaluateAsync(
        Guid hospitalId,
        Guid frameworkId,
        Guid? journeyId,
        CancellationToken cancellationToken = default)
    {
        var resolvedJourneyId = journeyId;
        if (!resolvedJourneyId.HasValue)
        {
            resolvedJourneyId = await _db.ComplianceJourneys
                .AsNoTracking()
                .Where(j => j.HospitalId == hospitalId && j.FrameworkId == frameworkId)
                .OrderByDescending(j => j.CreatedAt)
                .Select(j => (Guid?)j.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (!resolvedJourneyId.HasValue)
        {
            return new DeterministicNabhScoreResult
            {
                HospitalId = hospitalId,
                FrameworkId = frameworkId,
                JourneyId = null,
                CompliancePercent = 0,
                Pass = false,
                RiskScore = 100
            };
        }

        var chapters = await _db.ComplianceChapters
            .AsNoTracking()
            .Where(c => c.FrameworkId == frameworkId)
            .OrderBy(c => c.Sequence)
            .ToListAsync(cancellationToken);

        var standards = await _db.ComplianceStandards
            .AsNoTracking()
            .Where(s => s.Chapter.FrameworkId == frameworkId)
            .ToListAsync(cancellationToken);

        var standardIds = standards.Select(s => s.Id).ToList();
        var objectiveElements = await _db.ObjectiveElements
            .AsNoTracking()
            .Where(oe => standardIds.Contains(oe.ComplianceStandardId))
            .ToListAsync(cancellationToken);

        var oeIds = objectiveElements.Select(o => o.Id).ToList();
        var findings = await _db.Findings
            .AsNoTracking()
            .Where(f =>
                f.Scope == FindingScope.Audit &&
                f.ComplianceJourneyId == resolvedJourneyId &&
                f.ObjectiveElementId.HasValue &&
                oeIds.Contains(f.ObjectiveElementId.Value))
            .OrderByDescending(f => f.DateRaised)
            .ThenByDescending(f => f.CreatedAt)
            .ToListAsync(cancellationToken);

        var latestFindingByOe = findings
            .Where(f => f.ObjectiveElementId.HasValue)
            .GroupBy(f => f.ObjectiveElementId!.Value)
            .ToDictionary(g => g.Key, g => g.First());

        var chapterScores = new List<DeterministicChapterScore>();
        decimal frameworkWeighted = 0m;
        decimal frameworkWeightTotal = 0m;
        decimal riskAccumulator = 0m;

        foreach (var chapter in chapters)
        {
            var chapterStandards = standards.Where(s => s.ChapterId == chapter.Id).ToList();
            var standardScores = new List<DeterministicStandardScore>();

            decimal chapterWeighted = 0m;
            decimal chapterWeightTotal = 0m;
            bool chapterPass = true;

            foreach (var standard in chapterStandards)
            {
                var standardOes = objectiveElements.Where(oe => oe.ComplianceStandardId == standard.Id).ToList();
                if (standardOes.Count == 0)
                {
                    continue;
                }

                decimal standardWeighted = 0m;
                decimal standardWeightTotal = 0m;
                var mandatoryFailed = 0;

                foreach (var oe in standardOes)
                {
                    latestFindingByOe.TryGetValue(oe.Id, out var finding);
                    var status = finding?.ComplianceStatus ?? ComplianceEvaluationStatus.NonCompliant;
                    var remarks = finding?.Remarks;

                    var score = status switch
                    {
                        ComplianceEvaluationStatus.FullyCompliant => 1.0m,
                        ComplianceEvaluationStatus.PartiallyCompliant => 0.5m,
                        ComplianceEvaluationStatus.NotApplicable => string.IsNullOrWhiteSpace(remarks) ? 0m : -1m,
                        _ => 0m
                    };

                    if (oe.IsMandatory && status == ComplianceEvaluationStatus.NonCompliant)
                    {
                        mandatoryFailed++;
                    }

                    if (score >= 0m)
                    {
                        var weight = oe.Weight <= 0m ? 1m : oe.Weight;
                        standardWeighted += score * weight;
                        standardWeightTotal += weight;
                    }

                    riskAccumulator += finding?.RiskLevel switch
                    {
                        FindingRiskLevel.Critical => 4m,
                        FindingRiskLevel.High => 3m,
                        FindingRiskLevel.Medium => 2m,
                        FindingRiskLevel.Low => 1m,
                        _ => 2m
                    };
                }

                var standardPercent = standardWeightTotal > 0m
                    ? Math.Round((standardWeighted / standardWeightTotal) * 100m, 2)
                    : 0m;
                var standardPass = mandatoryFailed == 0;
                if (!standardPass)
                {
                    chapterPass = false;
                }

                standardScores.Add(new DeterministicStandardScore
                {
                    StandardId = standard.Id,
                    CompliancePercent = standardPercent,
                    Pass = standardPass,
                    MandatoryNonCompliantCount = mandatoryFailed
                });

                var standardWeight = standard.Weight <= 0m ? 1m : standard.Weight;
                chapterWeighted += standardPercent * standardWeight;
                chapterWeightTotal += standardWeight;
            }

            var chapterPercent = chapterWeightTotal > 0m
                ? Math.Round(chapterWeighted / chapterWeightTotal, 2)
                : 0m;
            if (chapterPercent < 80m)
            {
                chapterPass = false;
            }

            chapterScores.Add(new DeterministicChapterScore
            {
                ChapterId = chapter.Id,
                CompliancePercent = chapterPercent,
                Pass = chapterPass,
                Standards = standardScores
            });

            var chapterWeight = chapter.Weight <= 0m ? 1m : chapter.Weight;
            frameworkWeighted += chapterPercent * chapterWeight;
            frameworkWeightTotal += chapterWeight;
        }

        var frameworkPercent = frameworkWeightTotal > 0m
            ? Math.Round(frameworkWeighted / frameworkWeightTotal, 2)
            : 0m;
        var frameworkPass = chapterScores.All(c => c.Pass);
        var maxRisk = Math.Max(1, objectiveElements.Count);
        var riskScore = Math.Round(Math.Min(100m, (riskAccumulator / (maxRisk * 4m)) * 100m), 2);

        return new DeterministicNabhScoreResult
        {
            HospitalId = hospitalId,
            FrameworkId = frameworkId,
            JourneyId = resolvedJourneyId,
            CompliancePercent = frameworkPercent,
            Pass = frameworkPass,
            RiskScore = riskScore,
            Chapters = chapterScores
        };
    }
}
