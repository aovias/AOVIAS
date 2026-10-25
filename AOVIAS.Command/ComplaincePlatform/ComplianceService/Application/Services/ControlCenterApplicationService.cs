using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs.ControlCenter;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ComplianceService.Application.Services;

/// <summary>
/// Orchestrates the Compliance Command Center. Resolves the request scope (hospital/framework with
/// org-wide fallback), populates live signals, builds the aggregate overview and slices it for the
/// granular endpoints. The granular getters reuse the same overview build to guarantee consistency.
/// </summary>
public sealed class ControlCenterApplicationService : IControlCenterService
{
    private static readonly TimeSpan OverviewCacheDuration = TimeSpan.FromSeconds(60);

    private readonly ComplianceDbContext _db;
    private readonly IUserContextService _userContext;
    private readonly IControlCenterRepository _repository;
    private readonly IControlCenterAggregationService _aggregation;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ControlCenterApplicationService> _logger;

    public ControlCenterApplicationService(
        ComplianceDbContext db,
        IUserContextService userContext,
        IControlCenterRepository repository,
        IControlCenterAggregationService aggregation,
        IMemoryCache cache,
        ILogger<ControlCenterApplicationService> logger)
    {
        _db = db;
        _userContext = userContext;
        _repository = repository;
        _aggregation = aggregation;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ControlCenterOverviewDto> GetOverviewAsync(Guid? hospitalId, Guid? frameworkId, CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(hospitalId, frameworkId, cancellationToken);
        var cacheKey = BuildOverviewCacheKey(scope);
        if (_cache.TryGetValue(cacheKey, out ControlCenterOverviewDto? cached) && cached is not null)
        {
            _logger.LogDebug("Returning cached control center overview for {CacheKey}", cacheKey);
            return cached;
        }

        if (!scope.HasHospitals)
        {
            return new ControlCenterOverviewDto
            {
                Header = new ComplianceHeaderDto
                {
                    IsLive = true,
                    LastUpdated = DateTime.UtcNow,
                    OrganizationId = scope.OrganizationId.ToString()
                }
            };
        }

        var dataset = await _repository.LoadDatasetAsync(scope, cancellationToken);
        await _aggregation.PopulateSignalsAsync(scope, dataset, cancellationToken);
        var overview = await _aggregation.BuildOverviewAsync(scope, dataset, cancellationToken);
        _cache.Set(cacheKey, overview, OverviewCacheDuration);
        return overview;
    }

    private static string BuildOverviewCacheKey(ControlCenterScope scope)
        => $"control-center-overview:{scope.OrganizationId}:{scope.HospitalId}:{scope.FrameworkId}";

    public async Task<ComplianceHeaderDto> GetHeaderAsync(Guid? hospitalId, Guid? frameworkId, CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(hospitalId, frameworkId, cancellationToken);
        return new ComplianceHeaderDto
        {
            IsLive = true,
            LastUpdated = DateTime.UtcNow,
            OrganizationId = scope.OrganizationId.ToString(),
            OrganizationName = await _repository.GetOrganizationNameAsync(scope.OrganizationId, cancellationToken)
        };
    }

    public async Task<ComplianceIntelligenceDto> GetIntelligenceAsync(Guid? hospitalId, Guid? frameworkId, CancellationToken cancellationToken = default)
        => (await GetOverviewAsync(hospitalId, frameworkId, cancellationToken)).Intelligence;

    public async Task<List<RecommendedActionDto>> GetRecommendedActionsAsync(Guid? hospitalId, Guid? frameworkId, CancellationToken cancellationToken = default)
        => (await GetOverviewAsync(hospitalId, frameworkId, cancellationToken)).RecommendedActions;

    public async Task<ComplianceCommandCenterKpiDto> GetKpisAsync(Guid? hospitalId, Guid? frameworkId, CancellationToken cancellationToken = default)
        => (await GetOverviewAsync(hospitalId, frameworkId, cancellationToken)).Kpis;

    public async Task<List<HighRiskChapterDto>> GetHighRiskChaptersAsync(Guid? hospitalId, Guid? frameworkId, CancellationToken cancellationToken = default)
        => (await GetOverviewAsync(hospitalId, frameworkId, cancellationToken)).HighRiskChapters;

    public async Task<List<DepartmentRiskDto>> GetDepartmentRiskAsync(Guid? hospitalId, Guid? frameworkId, CancellationToken cancellationToken = default)
        => (await GetOverviewAsync(hospitalId, frameworkId, cancellationToken)).DepartmentRisks;

    public async Task<List<ActionCenterItemDto>> GetActionCenterAsync(Guid? hospitalId, Guid? frameworkId, CancellationToken cancellationToken = default)
        => (await GetOverviewAsync(hospitalId, frameworkId, cancellationToken)).ActionCenter;

    public async Task<List<ComplianceTrendPointDto>> GetTrendAsync(Guid? hospitalId, Guid? frameworkId, CancellationToken cancellationToken = default)
        => (await GetOverviewAsync(hospitalId, frameworkId, cancellationToken)).Trend;

    public async Task<List<ChapterComplianceDto>> GetChapterComplianceAsync(Guid? hospitalId, Guid? frameworkId, CancellationToken cancellationToken = default)
        => (await GetOverviewAsync(hospitalId, frameworkId, cancellationToken)).ChapterCompliance;

    public async Task<EvidenceCoverageDto> GetEvidenceCoverageAsync(Guid? hospitalId, Guid? frameworkId, CancellationToken cancellationToken = default)
        => (await GetOverviewAsync(hospitalId, frameworkId, cancellationToken)).EvidenceCoverage;

    public async Task<AuditReadinessDto> GetAuditReadinessAsync(Guid? hospitalId, Guid? frameworkId, CancellationToken cancellationToken = default)
        => (await GetOverviewAsync(hospitalId, frameworkId, cancellationToken)).AuditReadiness;

    public async Task<List<ActivityEventDto>> GetActivityStreamAsync(Guid? hospitalId, Guid? frameworkId, string? type, CancellationToken cancellationToken = default)
    {
        var activity = (await GetOverviewAsync(hospitalId, frameworkId, cancellationToken)).RecentActivity;
        if (string.IsNullOrWhiteSpace(type) || string.Equals(type, "all", StringComparison.OrdinalIgnoreCase))
        {
            return activity;
        }
        return activity.Where(a => string.Equals(a.Type, type, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public async Task<List<AlertDto>> GetAlertsAsync(Guid? hospitalId, Guid? frameworkId, CancellationToken cancellationToken = default)
        => (await GetOverviewAsync(hospitalId, frameworkId, cancellationToken)).Alerts;

    // ---------------------------------------------------------------------
    // Scope resolution (mirrors DashboardSnapshotService): explicit hospital or org-wide fallback.
    // ---------------------------------------------------------------------
    private async Task<ControlCenterScope> ResolveScopeAsync(Guid? hospitalId, Guid? frameworkId, CancellationToken cancellationToken)
    {
        var tenantId = _userContext.OrganizationId;
        if (tenantId == Guid.Empty)
        {
            throw new InvalidOperationException("Organization context is missing.");
        }

        var effectiveFrameworkId = await ResolveFrameworkIdAsync(tenantId, frameworkId, cancellationToken);
        var requestedHospitalId = (hospitalId.HasValue && hospitalId.Value != Guid.Empty)
            ? hospitalId.Value
            : (_userContext.HospitalId != Guid.Empty ? _userContext.HospitalId : (Guid?)null);

        var baseQuery = _db.ComplianceJourneys.AsNoTracking().Where(j => j.TenantId == tenantId);

        List<Guid> hospitalIds;
        if (requestedHospitalId.HasValue)
        {
            var exists = await baseQuery.AnyAsync(j => j.HospitalId == requestedHospitalId.Value, cancellationToken);
            hospitalIds = exists ? new List<Guid> { requestedHospitalId.Value } : new List<Guid>();
        }
        else
        {
            hospitalIds = await baseQuery.Select(j => j.HospitalId).Distinct().ToListAsync(cancellationToken);
        }

        return new ControlCenterScope
        {
            TenantId = tenantId,
            OrganizationId = tenantId,
            HospitalIds = hospitalIds,
            HospitalId = requestedHospitalId,
            FrameworkId = effectiveFrameworkId
        };
    }

    private async Task<Guid?> ResolveFrameworkIdAsync(Guid tenantId, Guid? requestedId, CancellationToken cancellationToken)
    {
        if (!requestedId.HasValue || requestedId.Value == Guid.Empty)
        {
            return null;
        }

        var isFramework = await _db.ComplianceFrameworks.AsNoTracking().AnyAsync(f => f.Id == requestedId.Value, cancellationToken);
        if (isFramework)
        {
            return requestedId.Value;
        }

        var journeyFrameworkId = await _db.ComplianceJourneys
            .AsNoTracking()
            .Where(j => j.TenantId == tenantId && j.Id == requestedId.Value)
            .Select(j => (Guid?)j.FrameworkId)
            .FirstOrDefaultAsync(cancellationToken);

        return journeyFrameworkId ?? requestedId.Value;
    }
}
