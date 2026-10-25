using ComplianceService.Application.Dashboard.Projection;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.Journey;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Platform.Dashboard.Models;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.Dashboard.Summary;

/// <summary>
/// Lightweight Phase 1 KPI reader: snapshots + task/finding aggregates (no live OE aggregation).
/// </summary>
public sealed class DashboardSummaryEngine : IDashboardSummaryEngine
{
    private static readonly TimeSpan RecentActivityWindow = TimeSpan.FromDays(7);

    private static readonly ComplianceLifecycleState[] ActiveJourneyStates =
    {
        ComplianceLifecycleState.GapAssessment,
        ComplianceLifecycleState.InProgress,
        ComplianceLifecycleState.Submitted,
        ComplianceLifecycleState.AuditScheduled,
        ComplianceLifecycleState.RenewalInProgress,
    };

    private readonly ComplianceDbContext _dbContext;
    private readonly IComplianceScoreService _scoreService;
    private readonly IDashboardSummaryCache _cache;
    private readonly IDashboardProjectionRepository _projectionRepository;
    private readonly DashboardProjectionOptions _projectionOptions;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<DashboardSummaryEngine> _logger;

    public DashboardSummaryEngine(
        ComplianceDbContext dbContext,
        IComplianceScoreService scoreService,
        IDashboardSummaryCache cache,
        IDashboardProjectionRepository projectionRepository,
        IOptions<DashboardProjectionOptions> projectionOptions,
        IHttpContextAccessor httpContextAccessor,
        ILogger<DashboardSummaryEngine> logger)
    {
        _dbContext = dbContext;
        _scoreService = scoreService;
        _cache = cache;
        _projectionRepository = projectionRepository;
        _projectionOptions = projectionOptions.Value;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<DashboardSummaryPayload> GetAsync(
        DashboardContext context,
        CancellationToken cancellationToken = default)
    {
        var telemetry = new DashboardSummaryTelemetry();
        using var scope = new DashboardSummaryTelemetryScope(telemetry);

        var tenantId = context.OrganizationId;
        var hospitalId = context.HospitalId;
        var frameworkId = context.FrameworkId;
        if (hospitalId is { } hid && hid != Guid.Empty
            && frameworkId is { } requestedFrameworkId && requestedFrameworkId != Guid.Empty)
        {
            frameworkId = await ComplianceJourneySnapshotResolver.ResolveFrameworkOrJourneyIdAsync(
                _dbContext,
                hid,
                requestedFrameworkId,
                cancellationToken);
        }
        scope.MarkContextResolution();

        if (context.ForceRefresh)
        {
            _cache.Invalidate(tenantId, hospitalId, frameworkId);
        }

        var cached = await _cache.TryGetAsync(tenantId, hospitalId, frameworkId, cancellationToken);
        scope.MarkCacheLookup(cached is not null);
        if (cached is not null)
        {
            StoreTelemetry(telemetry);
            return cached;
        }

        if (_projectionOptions.Enabled
            && hospitalId is { } projHospital
            && projHospital != Guid.Empty)
        {
            var projectionScope = DashboardProjectionScope.ForHospital(tenantId, projHospital, frameworkId);
            var projection = await _projectionRepository.GetAsync(projectionScope, cancellationToken);
            if (projection is not null && projection.IsComplete && !projection.IsCorrupted)
            {
                var projected = MapFromProjection(projection);
                await _cache.SetAsync(tenantId, hospitalId, frameworkId, projected, cancellationToken);
                StoreTelemetry(telemetry);
                return projected;
            }
        }

        var payload = await BuildPayloadAsync(tenantId, hospitalId, frameworkId, cancellationToken);
        scope.MarkQueryExecution();

        await _cache.SetAsync(tenantId, hospitalId, frameworkId, payload, cancellationToken);
        scope.MarkCachePopulation();

        StoreTelemetry(telemetry);

        if (telemetry.TotalMs > 200)
        {
            _logger.LogInformation(
                "Dashboard summary engine slow path: {TotalMs}ms CacheHit={CacheHit} HospitalId={HospitalId} Source={Source}",
                telemetry.TotalMs,
                telemetry.CacheHit,
                hospitalId,
                payload.Source);
        }

        return payload;
    }

    private async Task<DashboardSummaryPayload> BuildPayloadAsync(
        Guid tenantId,
        Guid? hospitalId,
        Guid? frameworkId,
        CancellationToken cancellationToken)
    {
        var journeysQuery = _dbContext.ComplianceJourneys
            .AsNoTracking()
            .Where(j => j.TenantId == tenantId);

        if (hospitalId is { } hid && hid != Guid.Empty)
        {
            journeysQuery = journeysQuery.Where(j => j.HospitalId == hid);
        }

        if (frameworkId is { } fid && fid != Guid.Empty)
        {
            journeysQuery = journeysQuery.Where(j => j.FrameworkId == fid);
        }

        // A scoped EF Core DbContext cannot execute multiple queries concurrently.
        // Keep dashboard reads sequential so the experience endpoint is reliable.
        var activeJourneysCount = await journeysQuery
            .CountAsync(j => ActiveJourneyStates.Contains(j.Status), cancellationToken);

        var hospitalsCount = await journeysQuery
            .Select(j => j.HospitalId)
            .Distinct()
            .CountAsync(cancellationToken);
        var (score, source) = await ResolveComplianceScoreAsync(hospitalId, frameworkId, cancellationToken);
        var pendingTasks = await CountPendingTasksAsync(hospitalId, frameworkId, cancellationToken);
        var alerts = await CountOpenAuditFindingsAsync(tenantId, hospitalId, frameworkId, cancellationToken);
        var lastActivityAt = await ResolveLastActivityAsync(tenantId, hospitalId, frameworkId, cancellationToken);
        if (hospitalId is { } scopedHospital && scopedHospital != Guid.Empty)
        {
            hospitalsCount = Math.Max(1, hospitalsCount);
        }

        return new DashboardSummaryPayload
        {
            OrganizationId = tenantId,
            HospitalId = hospitalId,
            FrameworkId = frameworkId,
            OverallCompliancePercent = score,
            ActiveJourneysCount = activeJourneysCount,
            PendingTasks = pendingTasks,
            AlertsCount = alerts,
            NotificationsCount = 0,
            HospitalsSummaryCount = hospitalsCount,
            LastActivityAt = lastActivityAt,
            RecentActivityIndicator = lastActivityAt.HasValue &&
                                      DateTime.UtcNow - lastActivityAt.Value <= RecentActivityWindow,
            LastUpdated = DateTimeOffset.UtcNow,
            Source = source,
        };
    }

    private async Task<(decimal Score, DashboardSummarySource Source)> ResolveComplianceScoreAsync(
        Guid? hospitalId,
        Guid? frameworkId,
        CancellationToken cancellationToken)
    {
        if (hospitalId is not { } hid || hid == Guid.Empty)
        {
            return (0m, DashboardSummarySource.Snapshot);
        }

        var snapshotQuery = _dbContext.ComplianceSnapshots
            .AsNoTracking()
            .Where(s => s.HospitalId == hid);

        if (frameworkId is { } fid && fid != Guid.Empty)
        {
            snapshotQuery = snapshotQuery.Where(s => s.FrameworkId == fid);

            var frameworkScore = await snapshotQuery
                .OrderByDescending(s => s.CalculatedAt)
                .Select(s => (decimal?)s.OverallScore)
                .FirstOrDefaultAsync(cancellationToken);

            if (frameworkScore.HasValue)
            {
                return (frameworkScore.Value, DashboardSummarySource.Snapshot);
            }

            var liveFramework = await _scoreService.GetFrameworkScoreAsync(hid, fid, cancellationToken);
            return (liveFramework.OverallScore, DashboardSummarySource.LiveFallback);
        }

        var latestPerFramework = await snapshotQuery
            .GroupBy(s => s.FrameworkId)
            .Select(g => g.OrderByDescending(s => s.CalculatedAt).Select(s => s.OverallScore).FirstOrDefault())
            .ToListAsync(cancellationToken);

        if (latestPerFramework.Count > 0)
        {
            return (Math.Round(latestPerFramework.Average(), 2), DashboardSummarySource.Snapshot);
        }

        var liveHospital = await _scoreService.GetHospitalScoreAsync(hid, cancellationToken);
        return (liveHospital.OverallScore, DashboardSummarySource.LiveFallback);
    }

    private async Task<int> CountPendingTasksAsync(
        Guid? hospitalId,
        Guid? frameworkId,
        CancellationToken cancellationToken)
    {
        if (hospitalId is not { } hid || hid == Guid.Empty)
        {
            return 0;
        }

        var query = _dbContext.ComplianceTasks
            .AsNoTracking()
            .Where(t => t.HospitalId == hid && t.Status != ComplianceTaskStatus.Completed);

        if (frameworkId is { } fid && fid != Guid.Empty)
        {
            query = query.Where(t =>
                t.ComplianceStandard != null &&
                t.ComplianceStandard.Chapter != null &&
                t.ComplianceStandard.Chapter.FrameworkId == fid);
        }

        return await query.CountAsync(cancellationToken);
    }

    private async Task<int> CountOpenAuditFindingsAsync(
        Guid tenantId,
        Guid? hospitalId,
        Guid? frameworkId,
        CancellationToken cancellationToken)
    {
        var query =
            from finding in _dbContext.Findings.AsNoTracking()
            join journey in _dbContext.ComplianceJourneys.AsNoTracking()
                on finding.ComplianceJourneyId equals journey.Id
            where finding.Scope == FindingScope.Audit
                  && !finding.IsResolved
                  && journey.TenantId == tenantId
            select new { finding, journey };

        if (hospitalId is { } hid && hid != Guid.Empty)
        {
            query = query.Where(x => x.journey.HospitalId == hid);
        }

        if (frameworkId is { } fid && fid != Guid.Empty)
        {
            query = query.Where(x => x.journey.FrameworkId == fid);
        }

        return await query.CountAsync(cancellationToken);
    }

    private async Task<DateTime?> ResolveLastActivityAsync(
        Guid tenantId,
        Guid? hospitalId,
        Guid? frameworkId,
        CancellationToken cancellationToken)
    {
        var historyQuery =
            from history in _dbContext.ComplianceJourneyHistories.AsNoTracking()
            join journey in _dbContext.ComplianceJourneys.AsNoTracking()
                on history.ComplianceJourneyId equals journey.Id
            where journey.TenantId == tenantId
            select new { history, journey };

        if (hospitalId is { } hid && hid != Guid.Empty)
        {
            historyQuery = historyQuery.Where(x => x.journey.HospitalId == hid);
        }

        if (frameworkId is { } fid && fid != Guid.Empty)
        {
            historyQuery = historyQuery.Where(x => x.journey.FrameworkId == fid);
        }

        return await historyQuery
            .Select(x => (DateTime?)x.history.ChangedAt)
            .MaxAsync(cancellationToken);
    }

    private static DashboardSummaryPayload MapFromProjection(DashboardProjection projection) =>
        new()
        {
            OrganizationId = projection.OrganizationId,
            HospitalId = projection.HospitalId == Guid.Empty ? null : projection.HospitalId,
            FrameworkId = projection.FrameworkId == Guid.Empty ? null : projection.FrameworkId,
            OverallCompliancePercent = projection.OverallComplianceScore,
            ActiveJourneysCount = projection.ActiveJourneysCount,
            PendingTasks = projection.PendingTasksCount,
            AlertsCount = projection.OpenAlertsCount,
            NotificationsCount = 0,
            HospitalsSummaryCount = 1,
            LastActivityAt = projection.LastActivityAt,
            RecentActivityIndicator = projection.LastActivityAt.HasValue &&
                                      DateTime.UtcNow - projection.LastActivityAt.Value <= RecentActivityWindow,
            LastUpdated = projection.LastUpdatedAt,
            Source = DashboardSummarySource.Snapshot,
        };

    private void StoreTelemetry(DashboardSummaryTelemetry telemetry)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            httpContext.Items[DashboardSummaryTelemetryKeys.HttpContextItemKey] = telemetry;
        }
    }
}
