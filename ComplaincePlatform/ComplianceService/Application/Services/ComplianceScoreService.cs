using ComplianceService.Application.Aggregation;
using ComplianceService.Application.ComplianceScoring;
using ComplianceService.Application.Contracts;
using ComplianceService.Domain;
using ComplianceService.Domain.Policies;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Domain.Enums;
using ComplianceScoreResult = ComplianceService.Application.ComplianceScoring.ComplianceScoreResult;

namespace ComplianceService.Application.Services;

/// <summary>
/// Canonical compliance scoring engine - the single source of truth for compliance percentages.
/// Computes live, OE-based scores (0-100) via <see cref="IAggregationService"/> using one fixed
/// classification policy so Dashboard, Journey, Sidebar and Reports all return identical values.
/// </summary>
public sealed class ComplianceScoreService : IComplianceScoreService
{
    private readonly ComplianceDbContext _context;
    private readonly IAggregationService _aggregation;
    private readonly IOperationalModeClassificationPolicy _classificationPolicy;
    private readonly ILogger<ComplianceScoreService> _logger;

    public ComplianceScoreService(
        ComplianceDbContext context,
        IAggregationService aggregation,
        IOperationalModeClassificationPolicy classificationPolicy,
        ILogger<ComplianceScoreService> logger)
    {
        _context = context;
        _aggregation = aggregation;
        _classificationPolicy = classificationPolicy;
        _logger = logger;
    }

    public async Task<decimal> GetJourneyScoreAsync(Guid frameworkId, Guid journeyId, CancellationToken cancellationToken = default)
    {
        if (frameworkId == Guid.Empty || journeyId == Guid.Empty)
        {
            return 0m;
        }

        var included = _classificationPolicy.GetClassificationsForDecisionRules(OperationalMode.FinalAudit);
        var agg = await _aggregation.ComputeFrameworkAsync(frameworkId, journeyId, included, cancellationToken);
        return agg.CompliancePercent;
    }

    public async Task<ComplianceScoreResult> GetFrameworkScoreAsync(Guid hospitalId, Guid frameworkId, CancellationToken cancellationToken = default)
    {
        var journey = await ResolveActiveJourneyAsync(hospitalId, frameworkId, cancellationToken);
        if (journey is null)
        {
            _logger.LogWarning("No journey found for hospital {HospitalId}, framework {FrameworkId}", hospitalId, frameworkId);
            return EmptyResult(hospitalId);
        }

        var included = _classificationPolicy.GetClassificationsForDecisionRules(OperationalMode.FinalAudit);
        var agg = await _aggregation.ComputeFrameworkAsync(frameworkId, journey.Id, included, cancellationToken);

        return new ComplianceScoreResult
        {
            HospitalId = hospitalId,
            OverallScore = agg.CompliancePercent,
            AuditConfidenceScore = agg.CompliancePercent,
            ChapterScores = agg.Chapters.Select(ch => new ChapterScoreDto
            {
                ChapterId = ch.ChapterId,
                WeightedScore = ch.CompliancePercent,
                CompletionPercentage = ch.CompliancePercent,
                RiskPenalty = 0
            }).ToList()
        };
    }

    public async Task<ComplianceScoreResult> GetHospitalScoreAsync(Guid hospitalId, CancellationToken cancellationToken = default)
    {
        var frameworkIds = await _context.Set<Regulation>()
            .Where(r => r.HospitalId == hospitalId)
            .Select(r => r.ComplianceFrameworkId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (frameworkIds.Count == 0)
        {
            return EmptyResult(hospitalId);
        }

        var included = _classificationPolicy.GetClassificationsForDecisionRules(OperationalMode.FinalAudit);
        var allChapterScores = new List<ChapterScoreDto>();
        decimal weightedSum = 0;
        int totalChapterCount = 0;

        foreach (var frameworkId in frameworkIds)
        {
            var journey = await ResolveActiveJourneyAsync(hospitalId, frameworkId, cancellationToken);
            if (journey is null)
            {
                continue;
            }

            var agg = await _aggregation.ComputeFrameworkAsync(frameworkId, journey.Id, included, cancellationToken);

            allChapterScores.AddRange(agg.Chapters.Select(ch => new ChapterScoreDto
            {
                ChapterId = ch.ChapterId,
                WeightedScore = ch.CompliancePercent,
                CompletionPercentage = ch.CompliancePercent,
                RiskPenalty = 0
            }));

            weightedSum += agg.CompliancePercent * agg.Chapters.Count;
            totalChapterCount += agg.Chapters.Count;
        }

        var overall = totalChapterCount > 0 ? Math.Round(weightedSum / totalChapterCount, 2) : 0m;

        return new ComplianceScoreResult
        {
            HospitalId = hospitalId,
            OverallScore = overall,
            AuditConfidenceScore = overall,
            ChapterScores = allChapterScores
        };
    }

    public async Task<decimal> GetStageScoreAsync(
        Guid journeyId,
        ComplianceService.Domain.ObjectiveElementClassification stage,
        CancellationToken cancellationToken = default)
    {
        if (journeyId == Guid.Empty) return 0m;

        // Join scores for the journey with OEs filtered to the requested classification band.
        // Uses the latest score per OE (highest AssessedAt) to avoid double-counting.
        var scores = await _context.ObjectiveElementScores
            .AsNoTracking()
            .Where(s => s.JourneyId == journeyId && s.IsApplicable)
            .Where(s => _context.Set<ObjectiveElement>()
                .Any(oe => oe.Id == s.ObjectiveElementId && oe.Classification == stage))
            .GroupBy(s => s.ObjectiveElementId)
            .Select(g => g.OrderByDescending(s => s.AssessedAt).First().Score)
            .ToListAsync(cancellationToken);

        if (scores.Count == 0) return 0m;

        // Convert 1–5 scores to 0–100 percentage (aligned with BuildOverview formula).
        var pct = Math.Round((decimal)scores.Average() / 5m * 100m, 2);
        return pct;
    }

    /// <summary>
    /// Deterministically resolves the active journey for a hospital+framework. A single, stable
    /// ordering (most recently updated, then most recently created) is used everywhere so all
    /// screens agree on which journey is "current".
    /// </summary>
    private Task<ComplianceJourney?> ResolveActiveJourneyAsync(Guid hospitalId, Guid frameworkId, CancellationToken cancellationToken)
        => _context.ComplianceJourneys
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(j => j.HospitalId == hospitalId && j.FrameworkId == frameworkId)
            .OrderByDescending(j => j.UpdatedAt)
            .ThenByDescending(j => j.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    private static ComplianceScoreResult EmptyResult(Guid hospitalId) => new()
    {
        HospitalId = hospitalId,
        OverallScore = 0,
        AuditConfidenceScore = 0,
        ChapterScores = new List<ChapterScoreDto>()
    };
}
