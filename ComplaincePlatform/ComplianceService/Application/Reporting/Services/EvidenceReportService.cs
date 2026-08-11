using ComplianceService.Application.Reporting.Contracts;
using ComplianceService.Application.Reporting.Dtos;
using ComplianceService.Application.Reporting.Export;
using ComplianceService.Domain;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ComplianceService.Application.Reporting.Services;

public sealed class EvidenceReportService : IEvidenceReportService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);

    private readonly ComplianceDbContext _db;
    private readonly IMemoryCache _cache;

    public EvidenceReportService(ComplianceDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<EvidenceReportDto> GetReportAsync(ReportFilter filter, CancellationToken cancellationToken = default)
    {
        var cacheKey = ReportCacheKeys.Build("evidence", filter);
        if (_cache.TryGetValue(cacheKey, out EvidenceReportDto? cached) && cached != null)
        {
            return cached;
        }

        var frameworkId = filter.FrameworkId ?? await ResolveFrameworkIdAsync(filter.HospitalId, cancellationToken);
        if (!frameworkId.HasValue)
        {
            return new EvidenceReportDto { GeneratedAt = DateTime.UtcNow };
        }

        var fwId = frameworkId.Value;
        var chapterIds = await _db.ComplianceChapters.AsNoTracking()
            .Where(c => c.FrameworkId == fwId)
            .Where(c => !filter.ChapterId.HasValue || c.Id == filter.ChapterId.Value)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        var standardIds = await _db.ComplianceStandards.AsNoTracking()
            .Where(s => chapterIds.Contains(s.ChapterId))
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        var oeIds = await _db.ObjectiveElements.AsNoTracking()
            .Where(oe => standardIds.Contains(oe.ComplianceStandardId) && oe.IsApplicable)
            .Select(oe => oe.Id)
            .ToListAsync(cancellationToken);

        var linksQuery = _db.ObjectiveElementEvidences.AsNoTracking()
            .Where(l => oeIds.Contains(l.ObjectiveElementId));

        if (filter.FromDate.HasValue)
        {
            linksQuery = linksQuery.Where(l => l.LinkedAt >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            linksQuery = linksQuery.Where(l => l.LinkedAt <= filter.ToDate.Value);
        }

        var links = await linksQuery
            .Select(l => new { l.ObjectiveElementId, l.VerificationStatus })
            .ToListAsync(cancellationToken);

        var approved = links.Count(l => l.VerificationStatus == EvidenceVerificationStatus.Verified);
        var pending = links.Count(l => l.VerificationStatus == EvidenceVerificationStatus.Pending);
        var rejected = links.Count(l => l.VerificationStatus == EvidenceVerificationStatus.Rejected);

        var clausesWithEvidence = links.Select(l => l.ObjectiveElementId).Distinct().Count();
        var totalClauses = oeIds.Count;
        var coverage = totalClauses > 0
            ? Math.Round((decimal)clausesWithEvidence / totalClauses * 100m, 2)
            : 0m;

        var oeChapterMap = await (
            from oe in _db.ObjectiveElements.AsNoTracking()
            join s in _db.ComplianceStandards.AsNoTracking() on oe.ComplianceStandardId equals s.Id
            join ch in _db.ComplianceChapters.AsNoTracking() on s.ChapterId equals ch.Id
            where oeIds.Contains(oe.Id)
            select new { OeId = oe.Id, ChapterId = ch.Id, ch.Code, ch.Name }
        ).ToDictionaryAsync(x => x.OeId, x => x, cancellationToken);

        var chapterEvidence = links
            .Where(l => oeChapterMap.ContainsKey(l.ObjectiveElementId))
            .GroupBy(l => oeChapterMap[l.ObjectiveElementId].ChapterId)
            .Select(g =>
            {
                var ch = oeChapterMap.Values.First(v => v.ChapterId == g.Key);
                var app = g.Count(x => x.VerificationStatus == EvidenceVerificationStatus.Verified);
                var pend = g.Count(x => x.VerificationStatus == EvidenceVerificationStatus.Pending);
                return new ChapterEvidenceDto
                {
                    ChapterId = ch.ChapterId,
                    ChapterCode = ch.Code,
                    ChapterName = ch.Name,
                    Approved = app,
                    Pending = pend,
                    Total = g.Count()
                };
            })
            .OrderByDescending(c => c.Total)
            .ToList();

        var report = new EvidenceReportDto
        {
            ApprovedEvidence = approved,
            PendingReview = pending,
            RejectedEvidence = rejected,
            CoverageRatio = coverage,
            ChapterEvidence = chapterEvidence,
            GeneratedAt = DateTime.UtcNow
        };

        _cache.Set(cacheKey, report, CacheTtl);
        return report;
    }

    public ReportExportModel BuildExportModel(EvidenceReportDto report, string? organizationName, string? frameworkName) =>
        new()
        {
            Title = "Evidence Report",
            OrganizationName = organizationName ?? string.Empty,
            FrameworkName = frameworkName ?? string.Empty,
            GeneratedAt = report.GeneratedAt,
            Kpis =
            [
                new ReportKpiItem { Label = "Approved", Value = report.ApprovedEvidence.ToString() },
                new ReportKpiItem { Label = "Pending Review", Value = report.PendingReview.ToString() },
                new ReportKpiItem { Label = "Rejected", Value = report.RejectedEvidence.ToString() },
                new ReportKpiItem { Label = "Coverage Ratio", Value = $"{report.CoverageRatio:F1}%" }
            ],
            Tables =
            [
                new ReportTableSection
                {
                    Title = "Evidence by Chapter",
                    Headers = ["Chapter", "Approved", "Pending", "Total"],
                    Rows = report.ChapterEvidence.Select(c => new List<string>
                    {
                        c.ChapterName, c.Approved.ToString(), c.Pending.ToString(), c.Total.ToString()
                    }).ToList()
                }
            ]
        };

    private async Task<Guid?> ResolveFrameworkIdAsync(Guid hospitalId, CancellationToken cancellationToken) =>
        await _db.ComplianceJourneys.AsNoTracking()
            .Where(j => j.HospitalId == hospitalId)
            .OrderByDescending(j => j.CreatedAt)
            .Select(j => (Guid?)j.FrameworkId)
            .FirstOrDefaultAsync(cancellationToken);
}
