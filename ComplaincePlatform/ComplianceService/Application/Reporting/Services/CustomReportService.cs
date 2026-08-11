using ComplianceService.Application.Reporting.Contracts;
using ComplianceService.Application.Reporting.Dtos;
using ComplianceService.Application.Reporting.Export;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.Reporting.Services;

public sealed class CustomReportService : ICustomReportService
{
    private static readonly HashSet<string> AllowedDimensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "Clause", "Chapter", "Department", "Tasks", "Evidence", "AuditFindings"
    };

    private readonly ComplianceDbContext _db;

    public CustomReportService(ComplianceDbContext db)
    {
        _db = db;
    }

    public async Task<CustomReportResultDto> GenerateAsync(
        ReportFilter filter,
        CustomReportRequest request,
        CancellationToken cancellationToken = default)
    {
        var dimensions = request.Dimensions
            .Where(d => AllowedDimensions.Contains(d.Trim()))
            .Select(d => d.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (dimensions.Count == 0)
        {
            dimensions = ["Clause", "Chapter"];
        }

        var frameworkId = request.FrameworkId ?? filter.FrameworkId
            ?? await ResolveFrameworkIdAsync(filter.HospitalId, cancellationToken);

        if (!frameworkId.HasValue)
        {
            return new CustomReportResultDto { GeneratedAt = DateTime.UtcNow };
        }

        var fwId = frameworkId.Value;
        var columns = new List<ColumnMetadataDto>();
        foreach (var dim in dimensions)
        {
            switch (dim.ToLowerInvariant())
            {
                case "clause":
                    columns.Add(new ColumnMetadataDto { Key = "clause", Label = "Clause", Type = "string" });
                    break;
                case "chapter":
                    columns.Add(new ColumnMetadataDto { Key = "chapter", Label = "Chapter", Type = "string" });
                    break;
                case "department":
                    columns.Add(new ColumnMetadataDto { Key = "department", Label = "Department", Type = "string" });
                    break;
                case "tasks":
                    columns.Add(new ColumnMetadataDto { Key = "tasks", Label = "Tasks", Type = "number" });
                    break;
                case "evidence":
                    columns.Add(new ColumnMetadataDto { Key = "evidence", Label = "Evidence", Type = "number" });
                    break;
                case "auditfindings":
                    columns.Add(new ColumnMetadataDto { Key = "auditFindings", Label = "Audit Findings", Type = "number" });
                    break;
            }
        }

        if (dimensions.Any(d => string.Equals(d, "Clause", StringComparison.OrdinalIgnoreCase)))
        {
            columns.Add(new ColumnMetadataDto { Key = "status", Label = "Status", Type = "string" });
        }

        var chapterFilter = request.ChapterId ?? filter.ChapterId;
        var baseQuery = from oe in _db.ObjectiveElements.AsNoTracking()
            join s in _db.ComplianceStandards.AsNoTracking() on oe.ComplianceStandardId equals s.Id
            join ch in _db.ComplianceChapters.AsNoTracking() on s.ChapterId equals ch.Id
            where ch.FrameworkId == fwId && oe.IsApplicable
            select new { oe, s, ch };

        if (chapterFilter.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.ch.Id == chapterFilter.Value);
        }

        var rowsData = await baseQuery
            .OrderBy(x => x.ch.Sequence)
            .ThenBy(x => x.oe.Code)
            .Take(500)
            .Select(x => new
            {
                x.oe.Id,
                x.oe.Code,
                x.oe.Title,
                x.oe.Department,
                ChapterCode = x.ch.Code,
                ChapterName = x.ch.Name,
                ChapterId = x.ch.Id
            })
            .ToListAsync(cancellationToken);

        var oeIds = rowsData.Select(r => r.Id).ToList();
        var journeyId = await _db.ComplianceJourneys.AsNoTracking()
            .Where(j => j.HospitalId == filter.HospitalId && j.FrameworkId == fwId)
            .OrderByDescending(j => j.CreatedAt)
            .Select(j => (Guid?)j.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var findingCounts = journeyId.HasValue
            ? await _db.Findings.AsNoTracking()
                .Where(f => f.ComplianceJourneyId == journeyId && f.ObjectiveElementId.HasValue && oeIds.Contains(f.ObjectiveElementId.Value))
                .GroupBy(f => f.ObjectiveElementId!.Value)
                .Select(g => new { OeId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.OeId, x => x.Count, cancellationToken)
            : new Dictionary<Guid, int>();

        var taskCounts = await _db.ComplianceTasks.AsNoTracking()
            .Where(t => t.HospitalId == filter.HospitalId && oeIds.Contains(t.ObjectiveElementId))
            .GroupBy(t => t.ObjectiveElementId)
            .Select(g => new { OeId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.OeId, x => x.Count, cancellationToken);

        var evidenceCounts = await _db.ObjectiveElementEvidences.AsNoTracking()
            .Where(l => oeIds.Contains(l.ObjectiveElementId))
            .GroupBy(l => l.ObjectiveElementId)
            .Select(g => new { OeId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.OeId, x => x.Count, cancellationToken);

        var latestStatus = journeyId.HasValue
            ? await _db.Findings.AsNoTracking()
                .Where(f => f.ComplianceJourneyId == journeyId && f.ObjectiveElementId.HasValue && oeIds.Contains(f.ObjectiveElementId.Value))
                .OrderByDescending(f => f.DateRaised)
                .Select(f => new { f.ObjectiveElementId, f.ComplianceStatus })
                .ToListAsync(cancellationToken)
            : [];

        var statusByOe = latestStatus
            .Where(f => f.ObjectiveElementId.HasValue)
            .GroupBy(f => f.ObjectiveElementId!.Value)
            .ToDictionary(g => g.Key, g => g.First().ComplianceStatus.ToString());

        var rows = new List<Dictionary<string, object?>>();
        foreach (var r in rowsData)
        {
            if (request.DepartmentId.HasValue)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(request.Severity))
            {
                // Severity filter applies at finding level — skip rows with no matching severity findings
                if (!findingCounts.ContainsKey(r.Id))
                {
                    continue;
                }
            }

            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            if (dimensions.Any(d => string.Equals(d, "Clause", StringComparison.OrdinalIgnoreCase)))
            {
                row["clause"] = r.Code;
                row["status"] = statusByOe.TryGetValue(r.Id, out var st)
                    ? FormatComplianceStatus(st)
                    : "NonCompliant";
            }

            if (dimensions.Any(d => string.Equals(d, "Chapter", StringComparison.OrdinalIgnoreCase)))
            {
                row["chapter"] = $"{r.ChapterCode} - {r.ChapterName}";
            }

            if (dimensions.Any(d => string.Equals(d, "Department", StringComparison.OrdinalIgnoreCase)))
            {
                row["department"] = r.Department ?? "Unassigned";
            }

            if (dimensions.Any(d => string.Equals(d, "Tasks", StringComparison.OrdinalIgnoreCase)))
            {
                row["tasks"] = taskCounts.GetValueOrDefault(r.Id);
            }

            if (dimensions.Any(d => string.Equals(d, "Evidence", StringComparison.OrdinalIgnoreCase)))
            {
                row["evidence"] = evidenceCounts.GetValueOrDefault(r.Id);
            }

            if (dimensions.Any(d => string.Equals(d, "AuditFindings", StringComparison.OrdinalIgnoreCase)))
            {
                row["auditFindings"] = findingCounts.GetValueOrDefault(r.Id);
            }

            rows.Add(row);
        }

        return new CustomReportResultDto
        {
            Columns = columns,
            Rows = rows,
            GeneratedAt = DateTime.UtcNow
        };
    }

    public ReportExportModel BuildExportModel(CustomReportResultDto report, string? organizationName, string? frameworkName) =>
        new()
        {
            Title = "Custom Report",
            OrganizationName = organizationName ?? string.Empty,
            FrameworkName = frameworkName ?? string.Empty,
            GeneratedAt = report.GeneratedAt,
            Tables =
            [
                new ReportTableSection
                {
                    Title = "Custom Report Data",
                    Headers = report.Columns.Select(c => c.Label).ToList(),
                    Rows = report.Rows.Select(row =>
                        report.Columns.Select(c => row.TryGetValue(c.Key, out var v) ? v?.ToString() ?? "" : "").ToList()
                    ).ToList()
                }
            ]
        };

    private static string FormatComplianceStatus(string status) =>
        status
            .Replace("FullyCompliant", "compliant", StringComparison.Ordinal)
            .Replace("PartiallyCompliant", "partial", StringComparison.Ordinal)
            .Replace("NonCompliant", "non-compliant", StringComparison.Ordinal);

    private async Task<Guid?> ResolveFrameworkIdAsync(Guid hospitalId, CancellationToken cancellationToken) =>
        await _db.ComplianceJourneys.AsNoTracking()
            .Where(j => j.HospitalId == hospitalId)
            .OrderByDescending(j => j.CreatedAt)
            .Select(j => (Guid?)j.FrameworkId)
            .FirstOrDefaultAsync(cancellationToken);
}
