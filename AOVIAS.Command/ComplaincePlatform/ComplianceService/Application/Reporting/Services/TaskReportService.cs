using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Application.Journey;
using ComplianceService.Application.Reporting.Contracts;
using ComplianceService.Application.Reporting.Dtos;
using ComplianceService.Application.Reporting.Export;
using ComplianceService.Application.TaskExecution;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ComplianceService.Application.Reporting.Services;

public sealed class TaskReportService : ITaskReportService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);

    private readonly ITaskQueryService _taskQuery;
    private readonly ComplianceDbContext _db;
    private readonly IUserContextService _userContext;
    private readonly IMemoryCache _cache;

    public TaskReportService(
        ITaskQueryService taskQuery,
        ComplianceDbContext db,
        IUserContextService userContext,
        IMemoryCache cache)
    {
        _taskQuery = taskQuery;
        _db = db;
        _userContext = userContext;
        _cache = cache;
    }

    public async Task<TaskReportDto> GetReportAsync(ReportFilter filter, CancellationToken cancellationToken = default)
    {
        var cacheKey = ReportCacheKeys.Build("tasks", filter);
        if (_cache.TryGetValue(cacheKey, out TaskReportDto? cached) && cached != null)
        {
            return cached;
        }

        var frameworkId = filter.FrameworkId ?? await ResolveFrameworkIdAsync(filter.HospitalId, cancellationToken);
        if (!frameworkId.HasValue || frameworkId.Value == Guid.Empty)
        {
            return new TaskReportDto { GeneratedAt = DateTime.UtcNow };
        }

        TaskApiStatus? status = null;
        if (!string.IsNullOrWhiteSpace(filter.TaskStatus) &&
            Enum.TryParse<TaskApiStatus>(filter.TaskStatus, true, out var parsed))
        {
            status = parsed;
        }

        var spec = new TaskQuerySpec
        {
            FrameworkId = frameworkId.Value,
            DepartmentId = filter.DepartmentId,
            ChapterId = filter.ChapterId,
            Status = status
        };

        var summary = await _taskQuery.GetSummaryAsync(spec, cancellationToken);
        var completionRate = summary.Total > 0
            ? Math.Round((decimal)summary.Completed / summary.Total * 100m, 2)
            : 0m;

        var hospitalId = filter.HospitalId;
        var fwId = await ComplianceJourneySnapshotResolver.ResolveFrameworkOrJourneyIdAsync(
            _db, hospitalId, frameworkId.Value, cancellationToken);

        var deptQuery = from t in _db.ComplianceTasks.AsNoTracking()
            join oe in _db.ObjectiveElements.AsNoTracking() on t.ObjectiveElementId equals oe.Id
            join s in _db.ComplianceStandards.AsNoTracking() on t.ComplianceStandardId equals s.Id
            join ch in _db.ComplianceChapters.AsNoTracking() on s.ChapterId equals ch.Id
            where t.HospitalId == hospitalId && ch.FrameworkId == fwId && oe.IsApplicable
            select t;

        if (filter.DepartmentId.HasValue)
        {
            deptQuery = deptQuery.Where(t => t.DepartmentId == filter.DepartmentId);
        }

        if (filter.FromDate.HasValue)
        {
            deptQuery = deptQuery.Where(t => t.CreatedAt >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            deptQuery = deptQuery.Where(t => t.CreatedAt <= filter.ToDate.Value);
        }

        var departmentTasks = await deptQuery
            .GroupBy(t => t.DepartmentName ?? "Unassigned")
            .Select(g => new DepartmentTaskDto { DepartmentName = g.Key, Count = g.Count() })
            .OrderByDescending(d => d.Count)
            .ToListAsync(cancellationToken);

        var report = new TaskReportDto
        {
            CompletionRate = completionRate,
            OverdueTasks = summary.Overdue,
            CompletedTasks = summary.Completed,
            InProgressTasks = summary.InProgress,
            DepartmentTasks = departmentTasks,
            GeneratedAt = DateTime.UtcNow
        };

        _cache.Set(cacheKey, report, CacheTtl);
        return report;
    }

    public ReportExportModel BuildExportModel(TaskReportDto report, string? organizationName, string? frameworkName) =>
        new()
        {
            Title = "Task Report",
            OrganizationName = organizationName ?? string.Empty,
            FrameworkName = frameworkName ?? string.Empty,
            GeneratedAt = report.GeneratedAt,
            Kpis =
            [
                new ReportKpiItem { Label = "Completion Rate", Value = $"{report.CompletionRate:F1}%" },
                new ReportKpiItem { Label = "Overdue", Value = report.OverdueTasks.ToString() },
                new ReportKpiItem { Label = "Completed", Value = report.CompletedTasks.ToString() },
                new ReportKpiItem { Label = "In Progress", Value = report.InProgressTasks.ToString() }
            ],
            Tables =
            [
                new ReportTableSection
                {
                    Title = "Tasks by Department",
                    Headers = ["Department", "Count"],
                    Rows = report.DepartmentTasks.Select(d => new List<string> { d.DepartmentName, d.Count.ToString() }).ToList()
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
