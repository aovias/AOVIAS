using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Application.ComplianceScoring;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Application.Services;

public class DashboardAppService : IDashboardAppService
{
    private readonly ComplianceDbContext _dbContext;
    private readonly IComplianceScoringService _scoringService;
    private readonly ComplianceAuditService _auditService;
    private readonly IComplianceAuditFindingRepository _findingRepository;
    private readonly ILogger<DashboardAppService> _logger;

    public DashboardAppService(
        ComplianceDbContext dbContext,
        IComplianceScoringService scoringService,
        ComplianceAuditService auditService,
        IComplianceAuditFindingRepository findingRepository,
        ILogger<DashboardAppService> logger)
    {
        _dbContext = dbContext;
        _scoringService = scoringService;
        _auditService = auditService;
        _findingRepository = findingRepository;
        _logger = logger;
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(
        Guid hospitalId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Building dashboard summary for HospitalId={HospitalId}", hospitalId);

        // Score: overall hospital score
        var score = await _scoringService.CalculateHospitalScoreAsync(hospitalId);

        // Task counts
        var tasksQuery = _dbContext.ComplianceTasks
            .AsNoTracking()
            .Where(t => t.HospitalId == hospitalId);

        var totalTasks = await tasksQuery.CountAsync(cancellationToken);
        var completedTasks = await tasksQuery
            .Where(t => t.Status == ComplianceTaskStatus.Completed)
            .CountAsync(cancellationToken);
        var pendingTasks = totalTasks - completedTasks;

        // Open audit findings: sum across journeys
        var journeys = await _dbContext.ComplianceJourneys
            .AsNoTracking()
            .Where(j => j.HospitalId == hospitalId && j.TenantId == tenantId)
            .Select(j => j.Id)
            .ToListAsync(cancellationToken);

        int openFindings = 0;
        if (journeys.Any())
        {
            openFindings = await _dbContext.Findings
                .AsNoTracking()
                .Where(f => f.Scope == FindingScope.Audit
                    && f.ComplianceJourneyId.HasValue
                    && journeys.Contains(f.ComplianceJourneyId.Value)
                    && !f.IsResolved)
                .CountAsync(cancellationToken);
        }

        // Upcoming renewals: reuse journey summary logic (approved journeys nearing expiry)
        var summary = await _auditService.GetJourneySummaryAsync(hospitalId, tenantId, null, cancellationToken);

        return new DashboardSummaryDto
        {
            HospitalId = hospitalId,
            ComplianceScore = score.OverallScore,
            TotalTasks = totalTasks,
            CompletedTasks = completedTasks,
            PendingTasks = pendingTasks,
            OpenAuditFindings = openFindings,
            UpcomingRenewals = summary.ExpiringIn90Days
        };
    }

    public async Task<HospitalDashboardDto> GetHospitalDashboardAsync(
        Guid hospitalId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Building hospital dashboard for HospitalId={HospitalId}", hospitalId);

        var score = await _scoringService.CalculateHospitalScoreAsync(hospitalId);
        var summary = await _auditService.GetJourneySummaryAsync(hospitalId, tenantId, null, cancellationToken);

        // Tasks with NABH hierarchy: ObjectiveElement → Standard → Chapter
        var tasks = await _dbContext.ComplianceTasks
            .AsNoTracking()
            .Where(t => t.HospitalId == hospitalId)
            .Include(t => t.ObjectiveElement)
                .ThenInclude(oe => oe!.ComplianceStandard)
                .ThenInclude(s => s!.Chapter)
            .ToListAsync(cancellationToken);

        // Department breakdown (best-effort)
        var departmentGroups = tasks
            .Where(t => t.ObjectiveElement != null)
            .Select(t => new
            {
                Task = t,
                ObjectiveElement = t.ObjectiveElement!
            })
            .GroupBy(x => x.ObjectiveElement.Department ?? "General");

        var departmentDtos = new List<DepartmentComplianceDto>();
        foreach (var grp in departmentGroups)
        {
            var deptTasks = grp.ToList();
            var total = deptTasks.Count;
            var completed = deptTasks.Count(x => x.Task.Status == ComplianceTaskStatus.Completed);

            var deptScore = total > 0 ? (decimal)completed / total * 100 : 0;

            departmentDtos.Add(new DepartmentComplianceDto
            {
                Department = grp.Key,
                ComplianceScore = Math.Round(deptScore, 2),
                TotalTasks = total,
                CompletedTasks = completed
            });
        }

        // Chapter breakdown
        var chapterGroups = tasks
            .Where(t => t.ObjectiveElement?.ComplianceStandard?.Chapter != null)
            .GroupBy(t => t.ObjectiveElement!.ComplianceStandard!.Chapter.Id);

        var chapterDtos = new List<ChapterComplianceDto>();
        foreach (var grp in chapterGroups)
        {
            var chapter = grp.First().ObjectiveElement!.ComplianceStandard!.Chapter;
            var total = grp.Count();
            var completed = grp.Count(x => x.Status == ComplianceTaskStatus.Completed);

            var chapterScore = total > 0 ? (decimal)completed / total * 100 : 0;

            chapterDtos.Add(new ChapterComplianceDto
            {
                ChapterId = chapter.Id,
                ChapterCode = chapter.Code,
                ChapterName = chapter.Name,
                ComplianceScore = Math.Round(chapterScore, 2),
                TotalTasks = total,
                CompletedTasks = completed
            });
        }

        return new HospitalDashboardDto
        {
            HospitalId = hospitalId,
            ComplianceScore = score.OverallScore,
            RiskIndex = summary.RiskIndex,
            DepartmentBreakdown = departmentDtos,
            ChapterBreakdown = chapterDtos
        };
    }

    public async Task<ComplianceTrendDto> GetComplianceTrendAsync(
        Guid hospitalId,
        Guid tenantId,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Building compliance trend for HospitalId={HospitalId}", hospitalId);

        var fromDate = from?.Date ?? DateTime.UtcNow.Date.AddDays(-30);
        var toDate = to?.Date ?? DateTime.UtcNow.Date;

        // Use ComplianceSnapshots as primary score source
        var snapshots = await _dbContext.ComplianceSnapshots
            .AsNoTracking()
            .Where(s => s.HospitalId == hospitalId
                        && s.CalculatedAt >= fromDate
                        && s.CalculatedAt <= toDate)
            .OrderBy(s => s.CalculatedAt)
            .ToListAsync(cancellationToken);

        // Task counts by date (simple grouping by CreatedAt date)
        var tasks = await _dbContext.ComplianceTasks
            .AsNoTracking()
            .Where(t => t.HospitalId == hospitalId
                        && t.CreatedAt.Date >= fromDate
                        && t.CreatedAt.Date <= toDate)
            .ToListAsync(cancellationToken);

        var points = new List<ComplianceTrendPointDto>();

        foreach (var snapshot in snapshots)
        {
            var date = snapshot.CalculatedAt.Date;

            var dayTasks = tasks.Where(t => t.CreatedAt.Date <= date).ToList();
            var completed = dayTasks.Count(t => t.Status == ComplianceTaskStatus.Completed);
            var pending = dayTasks.Count - completed;

            points.Add(new ComplianceTrendPointDto
            {
                Date = date,
                ComplianceScore = snapshot.OverallScore,
                CompletedTasks = completed,
                PendingTasks = pending
            });
        }

        return new ComplianceTrendDto
        {
            HospitalId = hospitalId,
            Points = points
        };
    }
}

