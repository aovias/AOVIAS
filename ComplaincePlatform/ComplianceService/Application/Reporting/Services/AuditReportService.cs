using ComplianceService.Application.Reporting.Contracts;
using ComplianceService.Application.Reporting.Dtos;
using ComplianceService.Application.Reporting.Export;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.Reporting.Services;

public sealed class AuditReportService : IAuditReportService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);

    private readonly ComplianceDbContext _db;
    private readonly IMemoryCache _cache;

    public AuditReportService(ComplianceDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<AuditReportDto> GetReportAsync(ReportFilter filter, CancellationToken cancellationToken = default)
    {
        var cacheKey = ReportCacheKeys.Build("audit", filter);
        if (_cache.TryGetValue(cacheKey, out AuditReportDto? cached) && cached != null)
        {
            return cached;
        }

        var journeyQuery = _db.ComplianceJourneys.AsNoTracking()
            .Where(j => j.HospitalId == filter.HospitalId);

        if (filter.FrameworkId.HasValue)
        {
            journeyQuery = journeyQuery.Where(j => j.FrameworkId == filter.FrameworkId.Value);
        }

        var journeyIds = await journeyQuery.Select(j => j.Id).ToListAsync(cancellationToken);

        var findingsQuery =
            from f in _db.Findings.AsNoTracking()
            join oe in _db.ObjectiveElements.AsNoTracking() on f.ObjectiveElementId equals oe.Id into oeJoin
            from oe in oeJoin.DefaultIfEmpty()
            where f.ComplianceJourneyId.HasValue && journeyIds.Contains(f.ComplianceJourneyId.Value)
            select new { f, oe };

        if (filter.FromDate.HasValue)
        {
            findingsQuery = findingsQuery.Where(x => x.f.DateRaised >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            findingsQuery = findingsQuery.Where(x => x.f.DateRaised <= filter.ToDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Severity) &&
            Enum.TryParse<FindingSeverity>(filter.Severity, true, out var sev))
        {
            findingsQuery = findingsQuery.Where(x => x.f.Severity == sev);
        }

        if (filter.DepartmentId.HasValue)
        {
            findingsQuery = findingsQuery.Where(x =>
                x.f.TaskId.HasValue &&
                _db.ComplianceTasks.Any(t => t.Id == x.f.TaskId && t.DepartmentId == filter.DepartmentId));
        }

        var findings = await findingsQuery
            .Select(x => new
            {
                x.f.Severity,
                x.f.Department,
                x.f.ObjectiveElementId,
                OeStandardId = x.oe != null ? (Guid?)x.oe.ComplianceStandardId : null
            })
            .ToListAsync(cancellationToken);

        var critical = findings.Count(f => f.Severity == FindingSeverity.Critical);
        var major = findings.Count(f => f.Severity == FindingSeverity.Major);
        var minor = findings.Count(f => f.Severity == FindingSeverity.Minor);

        var standardIds = findings.Where(f => f.OeStandardId.HasValue).Select(f => f.OeStandardId!.Value).Distinct().ToList();
        var standardChapterMap = await _db.ComplianceStandards.AsNoTracking()
            .Where(s => standardIds.Contains(s.Id))
            .Join(_db.ComplianceChapters.AsNoTracking(), s => s.ChapterId, ch => ch.Id, (s, ch) => new { StandardId = s.Id, ChapterId = ch.Id, ch.Code, ch.Name })
            .ToDictionaryAsync(x => x.StandardId, x => x, cancellationToken);

        var byChapter = findings
            .Where(f => f.OeStandardId.HasValue && standardChapterMap.ContainsKey(f.OeStandardId.Value))
            .GroupBy(f => standardChapterMap[f.OeStandardId!.Value].ChapterId)
            .Select(g =>
            {
                var ch = standardChapterMap.Values.First(v => v.ChapterId == g.Key);
                return new ChapterFindingDto
                {
                    ChapterId = ch.ChapterId,
                    ChapterCode = ch.Code,
                    ChapterName = ch.Name,
                    Critical = g.Count(x => x.Severity == FindingSeverity.Critical),
                    Major = g.Count(x => x.Severity == FindingSeverity.Major),
                    Minor = g.Count(x => x.Severity == FindingSeverity.Minor),
                    Total = g.Count()
                };
            })
            .Where(c => !filter.ChapterId.HasValue || c.ChapterId == filter.ChapterId.Value)
            .OrderByDescending(c => c.Total)
            .ToList();

        var byDept = findings
            .GroupBy(f => f.Department.ToString())
            .Select(g => new DepartmentFindingDto { Department = g.Key, Count = g.Count() })
            .OrderByDescending(d => d.Count)
            .ToList();

        var report = new AuditReportDto
        {
            CriticalFindings = critical,
            MajorFindings = major,
            MinorFindings = minor,
            TotalFindings = findings.Count,
            FindingsByChapter = byChapter,
            FindingsByDepartment = byDept,
            GeneratedAt = DateTime.UtcNow
        };

        _cache.Set(cacheKey, report, CacheTtl);
        return report;
    }

    public ReportExportModel BuildExportModel(AuditReportDto report, string? organizationName, string? frameworkName) =>
        new()
        {
            Title = "Audit Report",
            OrganizationName = organizationName ?? string.Empty,
            FrameworkName = frameworkName ?? string.Empty,
            GeneratedAt = report.GeneratedAt,
            Kpis =
            [
                new ReportKpiItem { Label = "Critical", Value = report.CriticalFindings.ToString() },
                new ReportKpiItem { Label = "Major", Value = report.MajorFindings.ToString() },
                new ReportKpiItem { Label = "Minor", Value = report.MinorFindings.ToString() },
                new ReportKpiItem { Label = "Total", Value = report.TotalFindings.ToString() }
            ],
            Tables =
            [
                new ReportTableSection
                {
                    Title = "Findings by Chapter",
                    Headers = ["Chapter", "Critical", "Major", "Minor", "Total"],
                    Rows = report.FindingsByChapter.Select(c => new List<string>
                    {
                        c.ChapterName, c.Critical.ToString(), c.Major.ToString(), c.Minor.ToString(), c.Total.ToString()
                    }).ToList()
                },
                new ReportTableSection
                {
                    Title = "Findings by Department",
                    Headers = ["Department", "Count"],
                    Rows = report.FindingsByDepartment.Select(d => new List<string> { d.Department, d.Count.ToString() }).ToList()
                }
            ]
        };
}
