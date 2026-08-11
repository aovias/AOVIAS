using WorkforceIntelligenceService.Application.Contracts;

namespace WorkforceIntelligenceService.Application.Intelligence.Platform;

public interface IIntelligenceProjectionHandler
{
    string EventType { get; }
    Task HandleAsync(IntelligenceEventEnvelope envelope, CancellationToken ct = default);
}

public interface IIntelligenceProjectionRegistry
{
    IReadOnlyList<IIntelligenceProjectionHandler> Handlers { get; }
    Task ProcessAsync(IntelligenceEventEnvelope envelope, CancellationToken ct = default);
}

public interface ISnapshotJob
{
    string JobName { get; }
    Task ExecuteAsync(Guid organizationId, string correlationId, CancellationToken ct = default);
}

public interface IWorkforceSnapshotService
{
    Task RefreshOrganizationAsync(Guid organizationId, string correlationId, bool fullReconcile = false, CancellationToken ct = default);
}

public interface IIntelligenceQueryService
{
    Task<ExecutiveDashboardDto> GetDashboardAsync(IntelligenceScope scope, CancellationToken ct = default);
    Task<HeadcountAnalyticsDto> GetHeadcountAsync(IntelligenceScope scope, CancellationToken ct = default);
    Task<SkillMatrixDto> GetSkillsAsync(IntelligenceScope scope, CancellationToken ct = default);
    Task<CompetencyMatrixDto> GetCompetenciesAsync(IntelligenceScope scope, CancellationToken ct = default);
    Task<TrainingMatrixDto> GetTrainingAsync(IntelligenceScope scope, CancellationToken ct = default);
    Task<LicenseDashboardDto> GetLicensesAsync(IntelligenceScope scope, CancellationToken ct = default);
    Task<CertificationDashboardDto> GetCertificationsAsync(IntelligenceScope scope, CancellationToken ct = default);
    Task<ForecastDto> GetForecastAsync(IntelligenceScope scope, string forecastType, CancellationToken ct = default);
    Task<RecommendationListDto> GetRecommendationsAsync(IntelligenceScope scope, CancellationToken ct = default);
    Task<WorkforceSearchResultDto> SearchAsync(IntelligenceScope scope, string query, int page, int pageSize, CancellationToken ct = default);
    Task<OrganizationAnalyticsDto> GetOrganizationAnalyticsAsync(IntelligenceScope scope, CancellationToken ct = default);
    Task<ResponsibilityMatrixDto> GetResponsibilitiesAsync(IntelligenceScope scope, CancellationToken ct = default);
    Task<CommitteeAnalyticsDto> GetCommitteesAsync(IntelligenceScope scope, CancellationToken ct = default);
    Task<AvailabilityAnalyticsDto> GetAvailabilityAsync(IntelligenceScope scope, CancellationToken ct = default);
}

public interface IForecastEngine
{
    Task<ForecastSnapshotResult> GenerateAsync(Guid organizationId, string forecastType, string correlationId, CancellationToken ct = default);
}

public record ForecastSnapshotResult(Guid SnapshotId, int Version, string ResultsJson);

public interface IRecommendationOrchestrator
{
    Task<int> GenerateAsync(Guid organizationId, string correlationId, CancellationToken ct = default);
}

public interface IIntelligenceOutboxWriter
{
    Task EnqueueAsync(string type, Guid aggregateId, object payload, CancellationToken ct = default);
}

public class IntelligenceOptions
{
    public const string SectionName = "Intelligence";
    public int SnapshotIntervalMinutes { get; set; } = 60;
    public int FullReconcileCronHours { get; set; } = 24;
    public int CacheTtlMinutes { get; set; } = 15;
    public int ForecastHorizonDays { get; set; } = 365;
    public int CredentialExpiryWarningDays { get; set; } = 90;
}

public interface IAnalyticsCacheService
{
    Task<T?> GetAsync<T>(string cacheKey, CancellationToken ct = default);
    Task SetAsync<T>(string cacheKey, T payload, TimeSpan ttl, CancellationToken ct = default);
    Task InvalidateOrganizationAsync(Guid organizationId, CancellationToken ct = default);
}
