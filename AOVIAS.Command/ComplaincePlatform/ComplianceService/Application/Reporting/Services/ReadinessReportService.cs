using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Application.Reporting.Contracts;
using ComplianceService.Application.Reporting.Dtos;
using ComplianceService.Application.Reporting.Export;
using ComplianceService.Application.TaskExecution;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Configs;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.Reporting.Services;

public sealed class ReadinessReportService : IReadinessReportService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);

    private readonly ComplianceDbContext _db;
    private readonly IDeterministicNabhScoringService _scoring;
    private readonly ITaskQueryService _taskQuery;
    private readonly ReadinessWeightOptions _weights;
    private readonly IMemoryCache _cache;

    public ReadinessReportService(
        ComplianceDbContext db,
        IDeterministicNabhScoringService scoring,
        ITaskQueryService taskQuery,
        IOptions<ReadinessWeightOptions> weights,
        IMemoryCache cache)
    {
        _db = db;
        _scoring = scoring;
        _taskQuery = taskQuery;
        _weights = weights.Value;
        _cache = cache;
    }

    public async Task<ReadinessReportDto> GetReportAsync(ReportFilter filter, CancellationToken cancellationToken = default)
    {
        var cacheKey = ReportCacheKeys.Build("readiness", filter);
        if (_cache.TryGetValue(cacheKey, out ReadinessReportDto? cached) && cached != null)
        {
            return cached;
        }

        var frameworkId = filter.FrameworkId ?? await ResolveNabhFrameworkIdAsync(cancellationToken);
        if (!frameworkId.HasValue)
        {
            return new ReadinessReportDto { GeneratedAt = DateTime.UtcNow };
        }

        var fwId = frameworkId.Value;
        var deterministic = await _scoring.EvaluateAsync(filter.HospitalId, fwId, null, cancellationToken);
        var compliancePct = deterministic.CompliancePercent;

        var standardIds = await (
            from s in _db.ComplianceStandards.AsNoTracking()
            join ch in _db.ComplianceChapters.AsNoTracking() on s.ChapterId equals ch.Id
            where ch.FrameworkId == fwId
            select s.Id
        ).ToListAsync(cancellationToken);

        var oeIds = await _db.ObjectiveElements.AsNoTracking()
            .Where(oe => standardIds.Contains(oe.ComplianceStandardId) && oe.IsApplicable)
            .Select(oe => oe.Id)
            .ToListAsync(cancellationToken);

        var verifiedClauseIds = await _db.ObjectiveElementEvidences.AsNoTracking()
            .Where(l => oeIds.Contains(l.ObjectiveElementId) && l.VerificationStatus == EvidenceVerificationStatus.Verified)
            .Select(l => l.ObjectiveElementId)
            .Distinct()
            .CountAsync(cancellationToken);

        var evidenceCoverage = oeIds.Count > 0
            ? Math.Round((decimal)verifiedClauseIds / oeIds.Count * 100m, 2)
            : 0m;

        var journeyIds = await _db.ComplianceJourneys.AsNoTracking()
            .Where(j => j.HospitalId == filter.HospitalId && j.FrameworkId == fwId)
            .Select(j => j.Id)
            .ToListAsync(cancellationToken);

        var openFindings = await _db.Findings.AsNoTracking()
            .CountAsync(f => f.ComplianceJourneyId.HasValue && journeyIds.Contains(f.ComplianceJourneyId.Value) && !f.IsResolved, cancellationToken);

        var totalFindings = await _db.Findings.AsNoTracking()
            .CountAsync(f => f.ComplianceJourneyId.HasValue && journeyIds.Contains(f.ComplianceJourneyId.Value), cancellationToken);

        var openFindingsPct = totalFindings > 0
            ? Math.Round((decimal)openFindings / totalFindings * 100m, 2)
            : 0m;

        var taskSummary = await _taskQuery.GetSummaryAsync(new TaskQuerySpec { FrameworkId = fwId }, cancellationToken);
        var taskCompletion = taskSummary.Total > 0
            ? Math.Round((decimal)taskSummary.Completed / taskSummary.Total * 100m, 2)
            : 0m;

        var (wC, wE, wF, wT) = _weights.Normalized();
        var readinessScore = Math.Round(
            wC * compliancePct +
            wE * evidenceCoverage +
            wF * (100m - openFindingsPct) +
            wT * taskCompletion,
            2);

        var chapters = await _db.ComplianceChapters.AsNoTracking()
            .Where(c => c.FrameworkId == fwId)
            .OrderBy(c => c.Sequence)
            .Select(c => new { c.Id, c.Code, c.Name })
            .ToListAsync(cancellationToken);

        var chapterScoreMap = deterministic.Chapters.ToDictionary(c => c.ChapterId, c => c.CompliancePercent);

        var highRisk = chapters
            .Select(ch => new { ch, Score = chapterScoreMap.GetValueOrDefault(ch.Id, 0m) })
            .OrderBy(x => x.Score)
            .Take(5)
            .Select(x => new HighRiskChapterDto
            {
                ChapterId = x.ch.Id,
                ChapterCode = x.ch.Code,
                ChapterName = x.ch.Name,
                Level = ReportComplianceScoring.RiskSeverityFromScore(x.Score),
                Details = $"Chapter {x.ch.Code} at {x.Score:F0}% compliance"
            })
            .ToList();

        var verifiedSet = await _db.ObjectiveElementEvidences.AsNoTracking()
            .Where(l => oeIds.Contains(l.ObjectiveElementId) && l.VerificationStatus == EvidenceVerificationStatus.Verified)
            .Select(l => l.ObjectiveElementId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var missing = await _db.ObjectiveElements.AsNoTracking()
            .Where(oe => oeIds.Contains(oe.Id) && !verifiedSet.Contains(oe.Id))
            .OrderBy(oe => oe.Code)
            .Take(20)
            .Select(oe => new MissingEvidenceDto
            {
                ObjectiveElementId = oe.Id,
                ClauseCode = oe.Code,
                Description = oe.Title
            })
            .ToListAsync(cancellationToken);

        var recommendations = new List<RecommendationDto>();
        if (missing.Count > 0)
        {
            recommendations.Add(new RecommendationDto
            {
                Text = $"Upload and verify evidence for {missing.Count} objective element(s) missing approved evidence.",
                Priority = "high"
            });
        }

        if (openFindings > 0)
        {
            recommendations.Add(new RecommendationDto
            {
                Text = $"Resolve {openFindings} open finding(s) before accreditation readiness review.",
                Priority = "high"
            });
        }

        if (taskSummary.Overdue > 0)
        {
            recommendations.Add(new RecommendationDto
            {
                Text = $"Address {taskSummary.Overdue} overdue compliance task(s).",
                Priority = "medium"
            });
        }

        if (readinessScore >= 80m)
        {
            recommendations.Add(new RecommendationDto
            {
                Text = "Overall readiness is strong; maintain evidence freshness and close remaining gaps.",
                Priority = "low"
            });
        }

        var report = new ReadinessReportDto
        {
            ReadinessScore = readinessScore,
            HighRiskChapters = highRisk,
            MissingEvidenceClauses = missing,
            Recommendations = recommendations,
            GeneratedAt = DateTime.UtcNow
        };

        _cache.Set(cacheKey, report, CacheTtl);
        return report;
    }

    public ReportExportModel BuildExportModel(ReadinessReportDto report, string? organizationName, string? frameworkName) =>
        new()
        {
            Title = "NABH Readiness Report",
            OrganizationName = organizationName ?? string.Empty,
            FrameworkName = frameworkName ?? string.Empty,
            GeneratedAt = report.GeneratedAt,
            Kpis = [new ReportKpiItem { Label = "Readiness Score", Value = $"{report.ReadinessScore:F1}%" }],
            Tables =
            [
                new ReportTableSection
                {
                    Title = "High Risk Chapters",
                    Headers = ["Chapter", "Level", "Details"],
                    Rows = report.HighRiskChapters.Select(c => new List<string> { c.ChapterName, c.Level, c.Details }).ToList()
                },
                new ReportTableSection
                {
                    Title = "Missing Evidence",
                    Headers = ["Clause", "Description"],
                    Rows = report.MissingEvidenceClauses.Select(m => new List<string> { m.ClauseCode, m.Description }).ToList()
                },
                new ReportTableSection
                {
                    Title = "Recommendations",
                    Headers = ["Priority", "Recommendation"],
                    Rows = report.Recommendations.Select(r => new List<string> { r.Priority, r.Text }).ToList()
                }
            ]
        };

    private async Task<Guid?> ResolveNabhFrameworkIdAsync(CancellationToken cancellationToken)
    {
        var id = await _db.ComplianceFrameworks.AsNoTracking()
            .Where(f => f.Name.StartsWith("NABH") || f.Authority.Contains("NABH"))
            .OrderBy(f => f.Name)
            .Select(f => (Guid?)f.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (id.HasValue)
        {
            return id;
        }

        return await _db.ComplianceFrameworks.AsNoTracking()
            .Where(f => f.IsActive)
            .Select(f => (Guid?)f.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
