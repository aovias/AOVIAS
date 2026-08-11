using ComplianceService.Application.ComplianceScoring;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.Aggregation;
using ComplianceService.Domain;
using ComplianceService.Domain.Policies;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Domain.Enums;
using System.Text.Json;
using DomainComplianceScoreResult = ComplianceService.Domain.ComplianceScoreResult;
using ApplicationComplianceScoreResult = ComplianceService.Application.ComplianceScoring.ComplianceScoreResult;

namespace ComplianceService.Application.Services;

/// <summary>
/// Service for calculating compliance scores using deterministic, weighted scoring.
/// Performs in-memory aggregation using EF queries without database snapshots or background jobs.
/// </summary>
public class ComplianceScoringService : Application.Contracts.IComplianceScoringService, Domain.IComplianceScoringService
{
    private readonly ComplianceDbContext _context;
    private readonly ILogger<ComplianceScoringService> _logger;
    private readonly IComplianceFrameworkSnapshotRecalculationService _snapshotRecalculation;
    private readonly IComplianceScoreService _complianceScore;

    public ComplianceScoringService(
        ComplianceDbContext context,
        ILogger<ComplianceScoringService> logger,
        IComplianceFrameworkSnapshotRecalculationService snapshotRecalculation,
        IComplianceScoreService complianceScore)
    {
        _context = context;
        _logger = logger;
        _snapshotRecalculation = snapshotRecalculation;
        _complianceScore = complianceScore;
    }

    public Task<ApplicationComplianceScoreResult> CalculateFrameworkScoreAsync(Guid hospitalId, Guid frameworkId)
    {
        // Delegate to the canonical single source of truth so every consumer
        // (Dashboard, Reports, Sidebar, Journey) shares identical numbers.
        return _complianceScore.GetFrameworkScoreAsync(hospitalId, frameworkId, CancellationToken.None);
    }

    public Task<ApplicationComplianceScoreResult> CalculateHospitalScoreAsync(Guid hospitalId)
    {
        // Delegate to the canonical single source of truth.
        return _complianceScore.GetHospitalScoreAsync(hospitalId, CancellationToken.None);
    }

    public async Task<ApplicationComplianceScoreResult> CalculateAndPersistHospitalScoreAsync(Guid hospitalId)
    {
        _logger.LogInformation(
            "Calculating and persisting (OE-based) compliance snapshots per framework for hospital {HospitalId}",
            hospitalId);

        var frameworkIds = await _context.ComplianceJourneys
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(j => j.HospitalId == hospitalId)
            .Select(j => j.FrameworkId)
            .Distinct()
            .ToListAsync();

        ApplicationComplianceScoreResult? last = null;

        foreach (var frameworkId in frameworkIds)
        {
            if (frameworkId == Guid.Empty)
            {
                _logger.LogWarning("Skipping snapshot persist: empty FrameworkId for hospital {HospitalId}", hospitalId);
                continue;
            }

            await _snapshotRecalculation.RecalculateAsync(hospitalId, frameworkId, CancellationToken.None);
            last = await CalculateFrameworkScoreAsync(hospitalId, frameworkId);
        }

        if (last != null)
        {
            return last;
        }

        return new ApplicationComplianceScoreResult
        {
            HospitalId = hospitalId,
            OverallScore = 0,
            AuditConfidenceScore = 0,
            MajorCount = 0,
            MinorCount = 0,
            CriticalCount = 0,
            ChapterScores = new List<ChapterScoreDto>()
        };
    }

    /// <summary>
    /// Domain interface implementation: Calculate score from context.
    /// </summary>
    public DomainComplianceScoreResult Calculate(ComplianceScoringContext context)
    {
        // Task status values that indicate completion
        var completedStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Completed", "Done", "Closed", "Verified"
        };

        // Calculate overall score from chapter scores
        decimal overallScore = 0;
        decimal totalWeight = 0;
        var chapterScores = new List<ChapterScoreResult>();

        foreach (var chapter in context.Chapters)
        {
            var chapterOeWork = context.ObjectiveElements.Where(oe => oe.ChapterId == chapter.ChapterId).ToList();
            var chapterRisks = context.Risks.Where(r => r.ChapterId == chapter.ChapterId).ToList();

            // OE-based chapter completion (one work row per objective element)
            int completedTasks = chapterOeWork.Count(oe => completedStatuses.Contains(oe.Status));
            int totalTasks = chapterOeWork.Count;
            decimal completionPercentage = totalTasks > 0 ? (decimal)completedTasks / totalTasks * 100 : 0;

            // Calculate risk penalty
            decimal riskPenalty = CalculateRiskPenaltyFromRisks(chapterRisks);

            // Chapter score after risk penalty
            decimal chapterScore = Math.Max(0, completionPercentage - riskPenalty);

            chapterScores.Add(new ChapterScoreResult(
                chapter.ChapterId,
                chapterScore,
                completedTasks,
                totalTasks,
                riskPenalty
            ));

            // Weight by chapter weight
            overallScore += chapterScore * chapter.Weight;
            totalWeight += chapter.Weight;
        }

        // Calculate weighted average
        decimal finalOverallScore = totalWeight > 0 ? overallScore / totalWeight : 0;
        finalOverallScore = Math.Clamp(finalOverallScore, 0, 100);

        // Calculate audit confidence (same as overall for now)
        decimal auditConfidence = finalOverallScore;

        return new DomainComplianceScoreResult(
            finalOverallScore,
            auditConfidence,
            chapterScores
        );
    }

    private decimal CalculateRiskPenaltyFromRisks(IReadOnlyCollection<RiskSnapshot> risks)
    {
        if (risks == null || !risks.Any())
            return 0;

        // Simple risk penalty calculation: sum of risk scores
        return risks.Sum(r => r.RiskScore);
    }
}
