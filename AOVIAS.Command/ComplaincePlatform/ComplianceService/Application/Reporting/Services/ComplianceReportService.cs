using ComplianceService.Application.Contracts;
using ComplianceService.Application.Journey;
using ComplianceService.Application.Reporting.Contracts;
using ComplianceService.Application.Reporting.Dtos;
using ComplianceService.Application.Reporting.Export;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ComplianceService.Application.Reporting.Services;

public sealed class ComplianceReportService : IComplianceReportService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);

    private readonly ComplianceDbContext _db;
    private readonly IDeterministicNabhScoringService _scoring;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ComplianceReportService> _logger;

    public ComplianceReportService(
        ComplianceDbContext db,
        IDeterministicNabhScoringService scoring,
        IMemoryCache cache,
        ILogger<ComplianceReportService> logger)
    {
        _db = db;
        _scoring = scoring;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ComplianceReportDto> GetReportAsync(ReportFilter filter, CancellationToken cancellationToken = default)
    {
        var cacheKey = ReportCacheKeys.Build("compliance", filter);
        if (_cache.TryGetValue(cacheKey, out ComplianceReportDto? cached) && cached != null)
        {
            return cached;
        }

        var frameworkId = await ResolveFrameworkIdAsync(filter, cancellationToken);
        if (!frameworkId.HasValue)
        {
            return EmptyReport();
        }

        var fwId = frameworkId.Value;
        var deterministic = await _scoring.EvaluateAsync(filter.HospitalId, fwId, null, cancellationToken);

        var chapters = await _db.ComplianceChapters.AsNoTracking()
            .Where(c => c.FrameworkId == fwId)
            .Where(c => !filter.ChapterId.HasValue || c.Id == filter.ChapterId.Value)
            .OrderBy(c => c.Sequence)
            .Select(c => new { c.Id, c.Code, c.Name })
            .ToListAsync(cancellationToken);

        var chapterIds = chapters.Select(c => c.Id).ToList();
        var standardIds = await _db.ComplianceStandards.AsNoTracking()
            .Where(s => chapterIds.Contains(s.ChapterId))
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        var objectiveElements = await _db.ObjectiveElements.AsNoTracking()
            .Where(oe => standardIds.Contains(oe.ComplianceStandardId) && oe.IsApplicable)
            .Select(oe => new { oe.Id, oe.ComplianceStandardId, oe.Weight })
            .ToListAsync(cancellationToken);

        var journeyId = deterministic.JourneyId;
        var oeIds = objectiveElements.Select(o => o.Id).ToList();

        var findings = journeyId.HasValue
            ? await _db.Findings.AsNoTracking()
                .Where(f => f.ComplianceJourneyId == journeyId && f.ObjectiveElementId.HasValue && oeIds.Contains(f.ObjectiveElementId.Value))
                .OrderByDescending(f => f.DateRaised)
                .ThenByDescending(f => f.CreatedAt)
                .Select(f => new { f.ObjectiveElementId, f.ComplianceStatus, f.Remarks })
                .ToListAsync(cancellationToken)
            : new();

        var latestByOe = findings
            .Where(f => f.ObjectiveElementId.HasValue)
            .GroupBy(f => f.ObjectiveElementId!.Value)
            .ToDictionary(g => g.Key, g => g.First());

        var compliant = 0;
        var partial = 0;
        var nonCompliant = 0;
        decimal weightedSum = 0m;
        decimal weightTotal = 0m;

        foreach (var oe in objectiveElements)
        {
            var status = latestByOe.TryGetValue(oe.Id, out var f)
                ? f.ComplianceStatus
                : ComplianceEvaluationStatus.NonCompliant;
            var remarks = f?.Remarks;
            var w = ReportComplianceScoring.StatusWeight(status, remarks);
            if (w < 0m)
            {
                continue;
            }

            var weight = oe.Weight <= 0m ? 1m : oe.Weight;
            weightedSum += w * weight;
            weightTotal += weight;

            switch (status)
            {
                case ComplianceEvaluationStatus.FullyCompliant:
                    compliant++;
                    break;
                case ComplianceEvaluationStatus.PartiallyCompliant:
                    partial++;
                    break;
                default:
                    nonCompliant++;
                    break;
            }
        }

        var total = objectiveElements.Count;
        var score = weightTotal > 0m
            ? Math.Round((weightedSum / weightTotal) * 100m, 2)
            : deterministic.CompliancePercent;

        var chapterScoreMap = deterministic.Chapters.ToDictionary(c => c.ChapterId, c => c.CompliancePercent);
        var chapterDtos = chapters.Select(ch =>
        {
            var chScore = chapterScoreMap.GetValueOrDefault(ch.Id, 0m);
            return new ChapterComplianceDto
            {
                ChapterId = ch.Id,
                ChapterCode = ch.Code,
                ChapterName = ch.Name,
                Score = chScore,
                Status = ReportComplianceScoring.ScoreToStatusLabel(chScore)
            };
        }).ToList();

        var trends = await LoadTrendsAsync(filter.HospitalId, fwId, filter.FromDate, filter.ToDate, cancellationToken);

        var topRisk = chapterDtos
            .OrderBy(c => c.Score)
            .Take(5)
            .Select(c => new RiskChapterDto
            {
                ChapterId = c.ChapterId,
                ChapterCode = c.ChapterCode,
                ChapterName = c.ChapterName,
                Score = c.Score,
                Severity = ReportComplianceScoring.RiskSeverityFromScore(c.Score),
                Details = $"{c.ChapterName} compliance at {c.Score:F0}%"
            })
            .ToList();

        var report = new ComplianceReportDto
        {
            TotalClauses = total,
            CompliantClauses = compliant,
            PartialClauses = partial,
            NonCompliantClauses = nonCompliant,
            ComplianceScore = score,
            Chapters = chapterDtos,
            Trends = trends,
            TopRiskChapters = topRisk,
            GeneratedAt = DateTime.UtcNow
        };

        _cache.Set(cacheKey, report, CacheTtl);
        _logger.LogInformation("Compliance report generated for hospital {HospitalId}", filter.HospitalId);
        return report;
    }

    public ReportExportModel BuildExportModel(ComplianceReportDto report, string? organizationName, string? frameworkName) =>
        new()
        {
            Title = "Compliance Report",
            OrganizationName = organizationName ?? string.Empty,
            FrameworkName = frameworkName ?? string.Empty,
            GeneratedAt = report.GeneratedAt,
            Kpis =
            [
                new ReportKpiItem { Label = "Total Clauses", Value = report.TotalClauses.ToString() },
                new ReportKpiItem { Label = "Compliant", Value = report.CompliantClauses.ToString() },
                new ReportKpiItem { Label = "Partial", Value = report.PartialClauses.ToString() },
                new ReportKpiItem { Label = "Non-Compliant", Value = report.NonCompliantClauses.ToString() },
                new ReportKpiItem { Label = "Compliance Score", Value = $"{report.ComplianceScore:F1}%" }
            ],
            Tables =
            [
                new ReportTableSection
                {
                    Title = "Compliance by Chapter",
                    Headers = ["Chapter", "Code", "Score %", "Status"],
                    Rows = report.Chapters.Select(c => new List<string>
                    {
                        c.ChapterName, c.ChapterCode, c.Score.ToString("F1"), c.Status
                    }).ToList()
                },
                new ReportTableSection
                {
                    Title = "Trend",
                    Headers = ["Date", "Score %"],
                    Rows = report.Trends.Select(t => new List<string>
                    {
                        t.Date.ToString("yyyy-MM-dd"), t.Value.ToString("F1")
                    }).ToList()
                }
            ]
        };

    private async Task<List<TrendPointDto>> LoadTrendsAsync(
        Guid hospitalId,
        Guid frameworkId,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken)
    {
        var q = _db.ComplianceSnapshots.AsNoTracking()
            .Where(s => s.HospitalId == hospitalId && s.FrameworkId == frameworkId);

        if (from.HasValue)
        {
            q = q.Where(s => s.CalculatedAt >= from.Value);
        }

        if (to.HasValue)
        {
            q = q.Where(s => s.CalculatedAt <= to.Value);
        }

        return await q
            .OrderBy(s => s.CalculatedAt)
            .Select(s => new TrendPointDto { Date = s.CalculatedAt, Value = s.OverallScore })
            .Take(24)
            .ToListAsync(cancellationToken);
    }

    private async Task<Guid?> ResolveFrameworkIdAsync(ReportFilter filter, CancellationToken cancellationToken)
    {
        if (filter.FrameworkId.HasValue && filter.FrameworkId.Value != Guid.Empty)
        {
            return filter.FrameworkId;
        }

        return await _db.ComplianceJourneys.AsNoTracking()
            .Where(j => j.HospitalId == filter.HospitalId)
            .OrderByDescending(j => j.CreatedAt)
            .Select(j => (Guid?)j.FrameworkId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static ComplianceReportDto EmptyReport() => new()
    {
        GeneratedAt = DateTime.UtcNow
    };
}
