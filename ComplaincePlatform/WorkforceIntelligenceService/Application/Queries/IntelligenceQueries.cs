using MediatR;
using WorkforceIntelligenceService.Application.Contracts;
using WorkforceIntelligenceService.Application.Intelligence.Platform;

namespace WorkforceIntelligenceService.Application.Queries;

public record GetExecutiveDashboardQuery(Guid OrganizationId, Guid? HospitalId, Guid? DepartmentId, DateTime? AsOf) : IRequest<ExecutiveDashboardDto>;
public record GetHeadcountAnalyticsQuery(Guid OrganizationId, Guid? HospitalId, Guid? DepartmentId, DateTime? AsOf) : IRequest<HeadcountAnalyticsDto>;
public record GetSkillMatrixQuery(Guid OrganizationId, Guid? HospitalId, Guid? DepartmentId, DateTime? AsOf) : IRequest<SkillMatrixDto>;
public record GetCompetencyMatrixQuery(Guid OrganizationId, Guid? HospitalId, Guid? DepartmentId, DateTime? AsOf) : IRequest<CompetencyMatrixDto>;
public record GetTrainingMatrixQuery(Guid OrganizationId, Guid? HospitalId, Guid? DepartmentId, DateTime? AsOf) : IRequest<TrainingMatrixDto>;
public record GetLicenseDashboardQuery(Guid OrganizationId, Guid? HospitalId, Guid? DepartmentId, DateTime? AsOf) : IRequest<LicenseDashboardDto>;
public record GetCertificationDashboardQuery(Guid OrganizationId, Guid? HospitalId, Guid? DepartmentId, DateTime? AsOf) : IRequest<CertificationDashboardDto>;
public record GetWorkforceForecastQuery(Guid OrganizationId, string ForecastType) : IRequest<ForecastDto>;
public record GetWorkforceRecommendationsQuery(Guid OrganizationId) : IRequest<RecommendationListDto>;
public record SearchWorkforceQuery(Guid OrganizationId, string Query, int Page, int PageSize) : IRequest<WorkforceSearchResultDto>;
public record GetOrganizationAnalyticsQuery(Guid OrganizationId, DateTime? AsOf) : IRequest<OrganizationAnalyticsDto>;
public record GetResponsibilityMatrixQuery(Guid OrganizationId, DateTime? AsOf) : IRequest<ResponsibilityMatrixDto>;
public record GetCommitteeAnalyticsQuery(Guid OrganizationId) : IRequest<CommitteeAnalyticsDto>;
public record GetAvailabilityAnalyticsQuery(Guid OrganizationId, Guid? HospitalId) : IRequest<AvailabilityAnalyticsDto>;

public record RefreshDashboardCommand(Guid OrganizationId) : IRequest<int>;
public record GenerateForecastCommand(Guid OrganizationId, string ForecastType) : IRequest<ForecastSnapshotResult>;
public record RebuildSearchIndexCommand(Guid OrganizationId) : IRequest<int>;
