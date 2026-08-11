using ComplianceService.Application.DTOs.ControlCenter;

namespace ComplianceService.Application.Contracts;

/// <summary>
/// Orchestrates the Compliance Command Center: resolves scope, populates live signals and builds
/// the aggregate overview plus the per-module slices consumed by the granular endpoints.
/// </summary>
public interface IControlCenterService
{
    Task<ControlCenterOverviewDto> GetOverviewAsync(Guid? hospitalId, Guid? frameworkId, CancellationToken cancellationToken = default);

    Task<ComplianceHeaderDto> GetHeaderAsync(Guid? hospitalId, Guid? frameworkId, CancellationToken cancellationToken = default);
    Task<ComplianceIntelligenceDto> GetIntelligenceAsync(Guid? hospitalId, Guid? frameworkId, CancellationToken cancellationToken = default);
    Task<List<RecommendedActionDto>> GetRecommendedActionsAsync(Guid? hospitalId, Guid? frameworkId, CancellationToken cancellationToken = default);
    Task<ComplianceCommandCenterKpiDto> GetKpisAsync(Guid? hospitalId, Guid? frameworkId, CancellationToken cancellationToken = default);
    Task<List<HighRiskChapterDto>> GetHighRiskChaptersAsync(Guid? hospitalId, Guid? frameworkId, CancellationToken cancellationToken = default);
    Task<List<DepartmentRiskDto>> GetDepartmentRiskAsync(Guid? hospitalId, Guid? frameworkId, CancellationToken cancellationToken = default);
    Task<List<ActionCenterItemDto>> GetActionCenterAsync(Guid? hospitalId, Guid? frameworkId, CancellationToken cancellationToken = default);
    Task<List<ComplianceTrendPointDto>> GetTrendAsync(Guid? hospitalId, Guid? frameworkId, CancellationToken cancellationToken = default);
    Task<List<ChapterComplianceDto>> GetChapterComplianceAsync(Guid? hospitalId, Guid? frameworkId, CancellationToken cancellationToken = default);
    Task<EvidenceCoverageDto> GetEvidenceCoverageAsync(Guid? hospitalId, Guid? frameworkId, CancellationToken cancellationToken = default);
    Task<AuditReadinessDto> GetAuditReadinessAsync(Guid? hospitalId, Guid? frameworkId, CancellationToken cancellationToken = default);
    Task<List<ActivityEventDto>> GetActivityStreamAsync(Guid? hospitalId, Guid? frameworkId, string? type, CancellationToken cancellationToken = default);
    Task<List<AlertDto>> GetAlertsAsync(Guid? hospitalId, Guid? frameworkId, CancellationToken cancellationToken = default);
}
