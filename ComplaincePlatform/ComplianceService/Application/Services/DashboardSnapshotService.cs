using ComplianceService.Application.Contracts;
using ComplianceService.Application.Dashboard.Projection;
using ComplianceService.Application.DTOs;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using Shared.Infrastructure.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using ComplianceService.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Infrastructure.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ComplianceService.Application.Services;

public sealed class DashboardSnapshotService : IDashboardSnapshotService, IDashboardSnapshotBuilder
{
    private static readonly TimeSpan SnapshotCacheDuration = TimeSpan.FromMinutes(2);

    private sealed class TaskSummaryRow
    {
        public Guid HospitalId { get; init; }
        public ComplianceTaskStatus Status { get; init; }
        public ComplianceTaskPriority Priority { get; init; }
        public string? MaturityState { get; init; }
        public DateTime? DueDate { get; init; }
        public Guid FrameworkId { get; init; }
        public string FrameworkCode { get; init; } = string.Empty;
    }

    private static readonly ComplianceLifecycleState[] ActiveJourneyStates =
    {
        ComplianceLifecycleState.GapAssessment,
        ComplianceLifecycleState.InProgress,
        ComplianceLifecycleState.Submitted,
        ComplianceLifecycleState.AuditScheduled,
        ComplianceLifecycleState.RenewalInProgress
    };

    private readonly ComplianceDbContext _dbContext;
    private readonly IUserContextService _userContext;
    private readonly IOrganizationServiceClient _organizationServiceClient;
    private readonly IComplianceScoreService _complianceScore;
    private readonly IMemoryCache _cache;
    private readonly IDashboardProjectionRepository _projectionRepository;
    private readonly DashboardProjectionOptions _projectionOptions;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<DashboardSnapshotService> _logger;

    public DashboardSnapshotService(
        ComplianceDbContext dbContext,
        IUserContextService userContext,
        IOrganizationServiceClient organizationServiceClient,
        IComplianceScoreService complianceScore,
        IMemoryCache cache,
        IDashboardProjectionRepository projectionRepository,
        IOptions<DashboardProjectionOptions> projectionOptions,
        IHttpContextAccessor httpContextAccessor,
        ILogger<DashboardSnapshotService> logger)
    {
        _dbContext = dbContext;
        _userContext = userContext;
        _organizationServiceClient = organizationServiceClient;
        _complianceScore = complianceScore;
        _cache = cache;
        _projectionRepository = projectionRepository;
        _projectionOptions = projectionOptions.Value;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<DashboardSnapshotDto> GetDashboardSnapshotAsync(
        Guid? hospitalId,
        Guid? frameworkId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _userContext.OrganizationId;
        if (tenantId == Guid.Empty)
        {
            throw new InvalidOperationException("Tenant context is missing.");
        }

        // The public parameter is named frameworkId, but the program-overview UI may supply a
        // ComplianceJourney (program) id instead. Resolve it to the underlying framework so the
        // snapshot is built whether the caller passes a framework id or a journey id.
        var effectiveFrameworkId = await ResolveFrameworkIdAsync(tenantId, frameworkId, cancellationToken);

        var resolvedHospitalId = ResolveHospitalId(hospitalId);
        var cacheKey = $"dashboard-snapshot:{tenantId}:{resolvedHospitalId}:{effectiveFrameworkId}";
        if (_cache.TryGetValue(cacheKey, out DashboardSnapshotDto? cachedSnapshot) && cachedSnapshot is not null)
        {
            _logger.LogDebug("Returning cached program overview snapshot for {CacheKey}", cacheKey);
            return cachedSnapshot;
        }

        var telemetry = new DashboardProjectionTelemetry();
        var readStopwatch = System.Diagnostics.Stopwatch.StartNew();

        if (_projectionOptions.Enabled && resolvedHospitalId is Guid scopedHospital && scopedHospital != Guid.Empty)
        {
            var scope = DashboardProjectionScope.ForHospital(
                tenantId,
                scopedHospital,
                effectiveFrameworkId);

            var projection = await _projectionRepository.GetAsync(scope, cancellationToken);
            if (projection is not null && projection.IsComplete && !projection.IsCorrupted)
            {
                var payload = DashboardProjectionSerializer.DeserializePayload(projection.PayloadJson);
                var organizationHospitals = await _organizationServiceClient.GetHospitalsAsync(tenantId, cancellationToken);
                var dto = DashboardProjectionSerializer.ToSnapshotDto(
                    projection,
                    payload,
                    tenantId,
                    organizationHospitals.FirstOrDefault(h => h.Id == scopedHospital)?.Name);

                readStopwatch.Stop();
                telemetry.UsedProjection = true;
                telemetry.ProjectionReadMs = readStopwatch.ElapsedMilliseconds;
                telemetry.ProjectionLastUpdatedAt = projection.LastUpdatedAt;
                telemetry.LastEventType = projection.LastEventType;
                StoreProjectionTelemetry(telemetry);

                _cache.Set(cacheKey, dto, SnapshotCacheDuration);
                _logger.LogDebug("Returning dashboard projection for scope {Scope}", scope);
                return dto;
            }
        }

        telemetry.UsedFallback = true;
        telemetry.FallbackCount = 1;
        var liveSnapshot = await BuildLiveAsync(tenantId, resolvedHospitalId, effectiveFrameworkId, cancellationToken);
        readStopwatch.Stop();
        telemetry.ProjectionReadMs = readStopwatch.ElapsedMilliseconds;
        StoreProjectionTelemetry(telemetry);

        _cache.Set(cacheKey, liveSnapshot, SnapshotCacheDuration);
        return liveSnapshot;
    }

    public async Task<DashboardSnapshotDto> BuildLiveAsync(
        Guid tenantId,
        Guid? hospitalId,
        Guid? frameworkId,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
        {
            throw new InvalidOperationException("Tenant context is missing.");
        }

        var effectiveFrameworkId = await ResolveFrameworkIdAsync(tenantId, frameworkId, cancellationToken);

        var hospitalIds = await ResolveHospitalIdsAsync(tenantId, hospitalId, cancellationToken);
        if (!hospitalIds.Any())
        {
            return BuildEmptySnapshot(tenantId);
        }

        var organizationHospitals = await _organizationServiceClient
            .GetHospitalsAsync(tenantId, cancellationToken);

        var journeys = await LoadJourneysAsync(tenantId, hospitalIds, effectiveFrameworkId, cancellationToken);
        var journeyIds = journeys.Select(j => j.Id).ToList();

        var journeyScores = await ComputeJourneyScoresAsync(journeys, cancellationToken);

        var tasks = await LoadTaskSummariesAsync(hospitalIds, effectiveFrameworkId, cancellationToken);

        var historyEntries = journeyIds.Any()
            ? await _dbContext.ComplianceJourneyHistories
                .AsNoTracking()
                .Where(h => journeyIds.Contains(h.ComplianceJourneyId))
                .OrderByDescending(h => h.ChangedAt)
                .Take(80)
                .ToListAsync(cancellationToken)
            : new List<ComplianceJourneyHistory>();

        var frameworks = BuildFrameworkSnapshots(journeys, historyEntries, journeyScores);
        var filteredHospitals = organizationHospitals
            .Where(h => hospitalIds.Contains(h.Id))
            .ToList();
        var summary = BuildSummary(journeys, tasks, filteredHospitals, frameworks.Count, journeyScores);
        var tasksSummary = BuildTasksSummary(tasks);
        var hospitals = BuildHospitalSnapshots(hospitalIds, journeys, tasks, filteredHospitals, journeyScores);
        var alerts = await LoadAlertsAsync(hospitalIds, effectiveFrameworkId, cancellationToken);
        var scoreTrends = await BuildScoreTrendsAsync(hospitalIds, effectiveFrameworkId, cancellationToken);
        var filters = BuildFilters(frameworks, hospitals, scoreTrends);

        return new DashboardSnapshotDto
        {
            IsSnapshotAvailable = journeys.Any(),
            SnapshotMaterialized = true,
            ComplianceJourneyId = journeys.FirstOrDefault()?.Id,
            FrameworkId = effectiveFrameworkId ?? journeys.FirstOrDefault()?.FrameworkId,
            OverallScore = summary.AvgComplianceScore,
            AuditConfidenceScore = summary.AvgComplianceScore,
            CalculatedAt = DateTime.UtcNow,
            TenantId = tenantId,
            GeneratedAt = DateTime.UtcNow.ToString("O"),
            Summary = summary,
            Frameworks = frameworks,
            Hospitals = hospitals,
            ScoreTrends = scoreTrends,
            TasksSummary = tasksSummary,
            Milestones = BuildMilestones(journeys, 8),
            Alerts = alerts,
            Filters = filters,
            SnapshotRange = filters.DateRange
        };
    }

    private void StoreProjectionTelemetry(DashboardProjectionTelemetry telemetry)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            httpContext.Items[DashboardProjectionTelemetryKeys.HttpContextItemKey] = telemetry;
        }
    }

    private Guid? ResolveHospitalId(Guid? requestedHospitalId)
    {
        if (requestedHospitalId.HasValue && requestedHospitalId.Value != Guid.Empty)
        {
            return requestedHospitalId.Value;
        }

        return _userContext.HospitalId != Guid.Empty
            ? _userContext.HospitalId
            : null;
    }

    private async Task<List<Guid>> ResolveHospitalIdsAsync(
        Guid tenantId,
        Guid? hospitalId,
        CancellationToken cancellationToken)
    {
        var baseQuery = _dbContext.ComplianceJourneys
            .AsNoTracking()
            .Where(j => j.TenantId == tenantId);

        if (hospitalId.HasValue)
        {
            var exists = await baseQuery.AnyAsync(j => j.HospitalId == hospitalId.Value, cancellationToken);
            return exists ? new List<Guid> { hospitalId.Value } : new List<Guid>();
        }

        return await baseQuery
            .Select(j => j.HospitalId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Resolves the ambiguous filter id supplied by the program-overview UI. The caller may pass
    /// either a real framework id or a ComplianceJourney (program) id; if it matches a journey for
    /// the tenant, the journey's framework is used. Unknown ids fall through unchanged so a genuine
    /// framework filter still works.
    /// </summary>
    private async Task<Guid?> ResolveFrameworkIdAsync(
        Guid tenantId,
        Guid? requestedId,
        CancellationToken cancellationToken)
    {
        if (!requestedId.HasValue || requestedId.Value == Guid.Empty)
        {
            return null;
        }

        var isFramework = await _dbContext.ComplianceFrameworks
            .AsNoTracking()
            .AnyAsync(f => f.Id == requestedId.Value, cancellationToken);
        if (isFramework)
        {
            return requestedId.Value;
        }

        var journeyFrameworkId = await _dbContext.ComplianceJourneys
            .AsNoTracking()
            .Where(j => j.TenantId == tenantId && j.Id == requestedId.Value)
            .Select(j => (Guid?)j.FrameworkId)
            .FirstOrDefaultAsync(cancellationToken);

        return journeyFrameworkId ?? requestedId.Value;
    }

    /// <summary>
    /// Computes the canonical compliance percentage (0-100) for every journey using the single
    /// source of truth so the Sidebar / program-overview agrees with Dashboard, Journey and Reports.
    /// </summary>
    private async Task<Dictionary<Guid, decimal>> ComputeJourneyScoresAsync(
        IReadOnlyCollection<ComplianceJourney> journeys,
        CancellationToken cancellationToken)
    {
        if (journeys.Count == 0)
        {
            return new Dictionary<Guid, decimal>();
        }

        var scorePairs = new List<(Guid Id, decimal score)>(journeys.Count);
        foreach (var journey in journeys)
        {
            var score = await _complianceScore.GetJourneyScoreAsync(
                journey.FrameworkId,
                journey.Id,
                cancellationToken);
            scorePairs.Add((journey.Id, score));
        }

        return scorePairs.ToDictionary(x => x.Id, x => x.score);
    }

    private static decimal ResolveScore(Dictionary<Guid, decimal> journeyScores, Guid journeyId)
        => journeyScores.TryGetValue(journeyId, out var score) ? score : 0m;

    private async Task<List<ComplianceJourney>> LoadJourneysAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> hospitalIds,
        Guid? frameworkId,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.ComplianceJourneys
            .AsNoTracking()
            .Where(j => j.TenantId == tenantId && hospitalIds.Contains(j.HospitalId));

        if (frameworkId.HasValue)
        {
            query = query.Where(j => j.FrameworkId == frameworkId.Value);
        }

        return await query
            .Include(j => j.Framework)
            .ToListAsync(cancellationToken);
    }

    private async Task<List<TaskSummaryRow>> LoadTaskSummariesAsync(
        IReadOnlyCollection<Guid> hospitalIds,
        Guid? frameworkId,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.ComplianceTasks
            .AsNoTracking()
            .Where(t => hospitalIds.Contains(t.HospitalId));

        if (frameworkId.HasValue)
        {
            query = query.Where(t => t.ComplianceStandard != null &&
                                     t.ComplianceStandard.Chapter != null &&
                                     t.ComplianceStandard.Chapter.FrameworkId == frameworkId.Value);
        }

        return await query
            .Select(t => new TaskSummaryRow
            {
                HospitalId = t.HospitalId,
                Status = t.Status,
                Priority = t.Priority,
                MaturityState = t.MaturityState,
                DueDate = t.DueDate,
                FrameworkId = t.ComplianceStandard!.Chapter!.FrameworkId,
                FrameworkCode = t.ComplianceStandard!.Chapter!.Framework != null &&
                                !string.IsNullOrWhiteSpace(t.ComplianceStandard.Chapter.Framework.PlatformId)
                    ? t.ComplianceStandard.Chapter.Framework.PlatformId
                    : t.ComplianceStandard.Chapter.Framework != null
                        ? t.ComplianceStandard.Chapter.Framework.Name
                        : t.ComplianceStandard.Chapter.FrameworkId.ToString()
            })
            .ToListAsync(cancellationToken);
    }

    private static List<FrameworkSnapshotDto> BuildFrameworkSnapshots(
        IReadOnlyCollection<ComplianceJourney> journeys,
        IReadOnlyCollection<ComplianceJourneyHistory> historyEntries,
        Dictionary<Guid, decimal> journeyScores)
    {
        var journeyGroups = journeys
            .GroupBy(j => j.FrameworkId)
            .ToList();

        var result = new List<FrameworkSnapshotDto>();
        foreach (var group in journeyGroups)
        {
            if (!group.Any())
            {
                continue;
            }

            var framework = group.First().Framework;
            var code = FormatFrameworkCode(framework, group.Key);
            var name = framework?.Name ?? code;
            var avgScore = Math.Round(group.Average(j => ResolveScore(journeyScores, j.Id)), 2);
            var statusCounts = group
                .GroupBy(j => j.Status.ToString())
                .ToDictionary(g => g.Key, g => g.Count());

            var milestoneGroup = BuildMilestones(group, 3);

            var journeyIds = group.Select(j => j.Id).ToHashSet();
            var recentActivities = historyEntries
                .Where(h => journeyIds.Contains(h.ComplianceJourneyId))
                .OrderByDescending(h => h.ChangedAt)
                .Take(6)
                .Select(h => new ActivitySnapshotDto
                {
                    ActivityId = h.Id.ToString(),
                    Actor = h.ChangedBy.ToString(),
                    CreatedAt = h.ChangedAt.ToString("O"),
                    Type = h.NewStatus.ToString(),
                    Summary = h.Remarks ?? h.NewStatus.ToString(),
                    FrameworkCode = code
                })
                .ToList();

            result.Add(new FrameworkSnapshotDto
            {
                FrameworkCode = code,
                FrameworkName = name,
                ActiveHospitals = group.Select(j => j.HospitalId).Distinct().Count(),
                Journeys = group.Count(),
                AvgScore = avgScore,
                StatusCounts = statusCounts,
                UpcomingMilestones = milestoneGroup,
                RecentActivity = recentActivities
            });
        }

        return result;
    }

    private static TasksSummaryDto BuildTasksSummary(IReadOnlyCollection<TaskSummaryRow> tasks)
    {
        var now = DateTime.UtcNow;
        var dueSoonThreshold = now.AddDays(14);

        var summary = new TasksSummaryDto
        {
            Total = tasks.Count,
            Open = tasks.Count(t => t.Status != ComplianceTaskStatus.Completed),
            Overdue = tasks.Count(t => t.DueDate.HasValue &&
                                      t.DueDate.Value.Date < now.Date &&
                                      t.Status != ComplianceTaskStatus.Completed),
            DueSoon = tasks.Count(t => t.DueDate.HasValue &&
                                      t.DueDate.Value.Date >= now.Date &&
                                      t.DueDate.Value.Date <= dueSoonThreshold.Date &&
                                      t.Status != ComplianceTaskStatus.Completed),
            ByPriority = tasks
                .GroupBy(t => t.Priority.ToString())
                .ToDictionary(g => g.Key, g => g.Count()),
            ByType = tasks
                .GroupBy(t => string.IsNullOrWhiteSpace(t.MaturityState) ? "Unknown" : t.MaturityState)
                .ToDictionary(g => g.Key, g => g.Count()),
            ByFramework = tasks
                .Where(t => t.FrameworkId != Guid.Empty)
                .GroupBy(t => t.FrameworkCode)
                .Select(g => new TasksByFrameworkDto
                {
                    FrameworkCode = g.Key,
                    Open = g.Count(t => t.Status != ComplianceTaskStatus.Completed),
                    Overdue = g.Count(t => t.DueDate.HasValue &&
                                           t.DueDate.Value.Date < now.Date &&
                                           t.Status != ComplianceTaskStatus.Completed)
                })
                .ToList()
        };

        return summary;
    }

    private static DashboardSnapshotSummaryDto BuildSummary(
        IReadOnlyCollection<ComplianceJourney> journeys,
        IReadOnlyCollection<TaskSummaryRow> tasks,
        IReadOnlyCollection<OrganizationHospitalDto> hospitals,
        int frameworkCount,
        Dictionary<Guid, decimal> journeyScores)
    {
        var now = DateTime.UtcNow;
        var overdueTasks = tasks.Count(t => t.DueDate.HasValue &&
                                           t.DueDate.Value.Date < now.Date &&
                                           t.Status != ComplianceTaskStatus.Completed);
        var avgScore = journeys.Any()
            ? Math.Round(journeys.Average(j => ResolveScore(journeyScores, j.Id)), 2)
            : 0m;
        // CompletionRate is reported on the same 0-100 scale as AvgComplianceScore.
        var completionRate = avgScore;
        var activeJourneys = journeys.Count(j => ActiveJourneyStates.Contains(j.Status));
        var upcomingExpiries = journeys.Count(j => j.ExpiryDate.HasValue &&
                                                   j.ExpiryDate.Value.Date >= now.Date &&
                                                   j.ExpiryDate.Value.Date <= now.AddDays(90).Date);

        return new DashboardSnapshotSummaryDto
        {
            TotalHospitals = hospitals.Count,
            OnboardedHospitals = hospitals.Count(h => h.IsOnboarded),
            TotalJourneys = journeys.Count,
            ActiveJourneys = activeJourneys,
            UpcomingExpiries = upcomingExpiries,
            OverdueTasks = overdueTasks,
            CompletionRate = completionRate,
            AvgComplianceScore = avgScore,
            FrameworksActive = frameworkCount
        };
    }

    private static List<HospitalSnapshotDto> BuildHospitalSnapshots(
        IReadOnlyCollection<Guid> hospitalIds,
        IReadOnlyCollection<ComplianceJourney> journeys,
        IReadOnlyCollection<TaskSummaryRow> tasks,
        IReadOnlyCollection<OrganizationHospitalDto> organizations,
        Dictionary<Guid, decimal> journeyScores)
    {
        var tasksByHospital = tasks
            .GroupBy(t => t.HospitalId)
            .ToDictionary(g => g.Key, g => g.ToList());
        var journeysByHospital = journeys
            .GroupBy(j => j.HospitalId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new List<HospitalSnapshotDto>();

        foreach (var hospitalId in hospitalIds)
        {
            var metadata = organizations.FirstOrDefault(h => h.Id == hospitalId);
            var hospitalTasks = tasksByHospital.TryGetValue(hospitalId, out var tList) ? tList : new List<TaskSummaryRow>();
            var openTasks = hospitalTasks.Count(t => t.Status != ComplianceTaskStatus.Completed);
            var overdueTasks = hospitalTasks.Count(t => t.DueDate.HasValue &&
                                                       t.DueDate.Value.Date < DateTime.UtcNow.Date &&
                                                       t.Status != ComplianceTaskStatus.Completed);
            var hospitalJourneys = journeysByHospital.TryGetValue(hospitalId, out var jList) ? jList : new List<ComplianceJourney>();
            var lastUpdated = hospitalJourneys.Any()
                ? hospitalJourneys.Max(j => j.UpdatedAt)
                : DateTime.UtcNow;

            result.Add(new HospitalSnapshotDto
            {
                HospitalId = hospitalId.ToString(),
                Name = metadata?.Name ?? $"Hospital {hospitalId:D8}",
                Code = metadata?.Code ?? string.Empty,
                IsOnboarded = metadata?.IsOnboarded ?? false,
                NabhLevel = metadata?.NabhLevel,
                OpenTasks = openTasks,
                OverdueTasks = overdueTasks,
                LastUpdated = FormatIso(lastUpdated),
                Journeys = hospitalJourneys.Select(j => MapJourneySnapshot(j, journeyScores)).ToList()
            });
        }

        return result;
    }

    private static JourneySnapshotDto MapJourneySnapshot(ComplianceJourney journey, Dictionary<Guid, decimal> journeyScores)
    {
        var score = ResolveScore(journeyScores, journey.Id);
        return new()
        {
            JourneyId = journey.Id.ToString(),
            FrameworkCode = FormatFrameworkCode(journey.Framework, journey.FrameworkId),
            FrameworkName = journey.Framework?.Name ?? string.Empty,
            StartDate = journey.EnrollmentDate?.ToString("O"),
            ExpiryDate = journey.ExpiryDate?.ToString("O"),
            Status = journey.Status.ToString(),
            Score = score,
            // ProgressPercent is reported on the same 0-100 scale as Score.
            ProgressPercent = score
        };
    }

    private static List<MilestoneSnapshotDto> BuildMilestones(
        IEnumerable<ComplianceJourney> journeys,
        int limit)
    {
        var now = DateTime.UtcNow;
        return journeys
            .Where(j => j.ExpiryDate.HasValue && j.ExpiryDate.Value >= now.AddDays(-7))
            .OrderBy(j => j.ExpiryDate)
            .Take(limit)
            .Select(j => new MilestoneSnapshotDto
            {
                MilestoneId = j.Id.ToString(),
                Title = $"{j.Framework?.Name ?? "Framework"} renewal",
                FrameworkCode = FormatFrameworkCode(j.Framework, j.FrameworkId),
                Owner = null,
                DueDate = j.ExpiryDate!.Value.ToString("O"),
                Status = j.Status.ToString()
            })
            .ToList();
    }

    private SnapshotFiltersDto BuildFilters(
        IReadOnlyCollection<FrameworkSnapshotDto> frameworks,
        IReadOnlyCollection<HospitalSnapshotDto> hospitals,
        IReadOnlyCollection<ScoreTrendDto> scoreTrends)
    {
        var dateRange = new SnapshotRangeDto
        {
            From = scoreTrends.FirstOrDefault()?.Date ?? DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd"),
            To = scoreTrends.LastOrDefault()?.Date ?? DateTime.UtcNow.ToString("yyyy-MM-dd")
        };

        return new SnapshotFiltersDto
        {
            Frameworks = frameworks.Select(f => f.FrameworkCode).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            Hospitals = hospitals.Select(h => new FilterHospitalDto
            {
                HospitalId = h.HospitalId,
                Name = h.Name
            }).ToList(),
            DateRange = dateRange
        };
    }

    private async Task<List<AlertSnapshotDto>> LoadAlertsAsync(
        IReadOnlyCollection<Guid> hospitalIds,
        Guid? frameworkId,
        CancellationToken cancellationToken)
    {
        // Phase 44 unified audit findings into the Findings table (TPH); do not query legacy ComplianceAuditFindings.
        var query = _dbContext.Findings
            .AsNoTracking()
            .Include(f => f.ComplianceJourney)
                .ThenInclude(j => j.Framework)
            .Where(f => f.Scope == FindingScope.Audit &&
                        !f.IsResolved &&
                        f.ComplianceJourney != null &&
                        hospitalIds.Contains(f.ComplianceJourney.HospitalId));

        if (frameworkId.HasValue)
        {
            query = query.Where(f => f.ComplianceJourney!.FrameworkId == frameworkId.Value);
        }

        var findings = await query
            .OrderByDescending(f => f.DateRaised)
            .Take(10)
            .ToListAsync(cancellationToken);

        return findings.Select(f => new AlertSnapshotDto
        {
            AlertId = f.Id.ToString(),
            Type = f.Type?.ToString() ?? "Finding",
            Severity = f.Severity.ToString(),
            Actor = f.CreatedByType.ToString(),
            CreatedAt = f.DateRaised.ToString("O"),
            Message = string.IsNullOrWhiteSpace(f.Title) ? f.Description : f.Title,
            EntityType = "Finding",
            EntityId = f.Id.ToString(),
            Acknowledged = f.IsResolved
        }).ToList();
    }

    private async Task<List<ScoreTrendDto>> BuildScoreTrendsAsync(
        IReadOnlyCollection<Guid> hospitalIds,
        Guid? frameworkId,
        CancellationToken cancellationToken)
    {
        var snapshotQuery = _dbContext.ComplianceSnapshots
            .AsNoTracking()
            .Where(s => hospitalIds.Contains(s.HospitalId));

        if (frameworkId.HasValue)
        {
            snapshotQuery = snapshotQuery.Where(s => s.FrameworkId == frameworkId.Value);
        }

        var snapshots = await snapshotQuery
            .OrderByDescending(s => s.CalculatedAt)
            .Take(60)
            .ToListAsync(cancellationToken);

        if (!snapshots.Any())
        {
            return new List<ScoreTrendDto>();
        }

        var frameworkIds = snapshots.Select(s => s.FrameworkId).Distinct().Where(id => id != Guid.Empty).ToList();
        var frameworks = await _dbContext.ComplianceFrameworks
            .AsNoTracking()
            .Where(f => frameworkIds.Contains(f.Id))
            .ToDictionaryAsync(f => f.Id, f => FormatFrameworkCode(f, f.Id), cancellationToken);

        return snapshots
            .GroupBy(s => s.CalculatedAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new ScoreTrendDto
            {
                Date = g.Key.ToString("yyyy-MM-dd"),
                AvgScore = Math.Round(g.Average(s => s.OverallScore), 2),
                Frameworks = g
                    .GroupBy(s => s.FrameworkId)
                    .Where(g2 => g2.Key != Guid.Empty)
                    .ToDictionary(
                        g2 => frameworks.GetValueOrDefault(g2.Key, g2.Key.ToString()),
                        g2 => Math.Round(g2.Average(s => s.OverallScore), 2))
            })
            .ToList();
    }

    private static DashboardSnapshotDto BuildEmptySnapshot(Guid tenantId)
        => new()
        {
            TenantId = tenantId,
            GeneratedAt = DateTime.UtcNow.ToString("O"),
            Summary = new DashboardSnapshotSummaryDto(),
            TasksSummary = new TasksSummaryDto()
        };

    private static string FormatIso(DateTime value) => value.ToString("O");

    private static string FormatFrameworkCode(ComplianceFramework? framework, Guid fallbackId)
        => framework != null && !string.IsNullOrWhiteSpace(framework.PlatformId)
            ? framework.PlatformId
            : framework?.Name ?? fallbackId.ToString();
}
