using MediatR;
using WorkforceIntelligenceService.Application.Contracts;
using WorkforceIntelligenceService.Application.Intelligence.Platform;
using WorkforceIntelligenceService.Application.Queries;

namespace WorkforceIntelligenceService.Application.Handlers;

public sealed class IntelligenceQueryHandlers(IIntelligenceQueryService queries) :
    IRequestHandler<GetExecutiveDashboardQuery, ExecutiveDashboardDto>,
    IRequestHandler<GetHeadcountAnalyticsQuery, HeadcountAnalyticsDto>,
    IRequestHandler<GetSkillMatrixQuery, SkillMatrixDto>,
    IRequestHandler<GetCompetencyMatrixQuery, CompetencyMatrixDto>,
    IRequestHandler<GetTrainingMatrixQuery, TrainingMatrixDto>,
    IRequestHandler<GetLicenseDashboardQuery, LicenseDashboardDto>,
    IRequestHandler<GetCertificationDashboardQuery, CertificationDashboardDto>,
    IRequestHandler<GetWorkforceForecastQuery, ForecastDto>,
    IRequestHandler<GetWorkforceRecommendationsQuery, RecommendationListDto>,
    IRequestHandler<SearchWorkforceQuery, WorkforceSearchResultDto>,
    IRequestHandler<GetOrganizationAnalyticsQuery, OrganizationAnalyticsDto>,
    IRequestHandler<GetResponsibilityMatrixQuery, ResponsibilityMatrixDto>,
    IRequestHandler<GetCommitteeAnalyticsQuery, CommitteeAnalyticsDto>,
    IRequestHandler<GetAvailabilityAnalyticsQuery, AvailabilityAnalyticsDto>
{
    private static IntelligenceScope Scope(Guid orgId, Guid? hospital, Guid? dept, DateTime? asOf)
        => new(orgId, hospital, dept, asOf);

    public Task<ExecutiveDashboardDto> Handle(GetExecutiveDashboardQuery r, CancellationToken ct)
        => queries.GetDashboardAsync(Scope(r.OrganizationId, r.HospitalId, r.DepartmentId, r.AsOf), ct);
    public Task<HeadcountAnalyticsDto> Handle(GetHeadcountAnalyticsQuery r, CancellationToken ct)
        => queries.GetHeadcountAsync(Scope(r.OrganizationId, r.HospitalId, r.DepartmentId, r.AsOf), ct);
    public Task<SkillMatrixDto> Handle(GetSkillMatrixQuery r, CancellationToken ct)
        => queries.GetSkillsAsync(Scope(r.OrganizationId, r.HospitalId, r.DepartmentId, r.AsOf), ct);
    public Task<CompetencyMatrixDto> Handle(GetCompetencyMatrixQuery r, CancellationToken ct)
        => queries.GetCompetenciesAsync(Scope(r.OrganizationId, r.HospitalId, r.DepartmentId, r.AsOf), ct);
    public Task<TrainingMatrixDto> Handle(GetTrainingMatrixQuery r, CancellationToken ct)
        => queries.GetTrainingAsync(Scope(r.OrganizationId, r.HospitalId, r.DepartmentId, r.AsOf), ct);
    public Task<LicenseDashboardDto> Handle(GetLicenseDashboardQuery r, CancellationToken ct)
        => queries.GetLicensesAsync(Scope(r.OrganizationId, r.HospitalId, r.DepartmentId, r.AsOf), ct);
    public Task<CertificationDashboardDto> Handle(GetCertificationDashboardQuery r, CancellationToken ct)
        => queries.GetCertificationsAsync(Scope(r.OrganizationId, r.HospitalId, r.DepartmentId, r.AsOf), ct);
    public Task<ForecastDto> Handle(GetWorkforceForecastQuery r, CancellationToken ct)
        => queries.GetForecastAsync(new IntelligenceScope(r.OrganizationId), r.ForecastType, ct);
    public Task<RecommendationListDto> Handle(GetWorkforceRecommendationsQuery r, CancellationToken ct)
        => queries.GetRecommendationsAsync(new IntelligenceScope(r.OrganizationId), ct);
    public Task<WorkforceSearchResultDto> Handle(SearchWorkforceQuery r, CancellationToken ct)
        => queries.SearchAsync(new IntelligenceScope(r.OrganizationId), r.Query, r.Page, r.PageSize, ct);
    public Task<OrganizationAnalyticsDto> Handle(GetOrganizationAnalyticsQuery r, CancellationToken ct)
        => queries.GetOrganizationAnalyticsAsync(new IntelligenceScope(r.OrganizationId, AsOf: r.AsOf), ct);
    public Task<ResponsibilityMatrixDto> Handle(GetResponsibilityMatrixQuery r, CancellationToken ct)
        => queries.GetResponsibilitiesAsync(new IntelligenceScope(r.OrganizationId, AsOf: r.AsOf), ct);
    public Task<CommitteeAnalyticsDto> Handle(GetCommitteeAnalyticsQuery r, CancellationToken ct)
        => queries.GetCommitteesAsync(new IntelligenceScope(r.OrganizationId), ct);
    public Task<AvailabilityAnalyticsDto> Handle(GetAvailabilityAnalyticsQuery r, CancellationToken ct)
        => queries.GetAvailabilityAsync(new IntelligenceScope(r.OrganizationId, r.HospitalId), ct);
}

public sealed class IntelligenceCommandHandlers(
    IWorkforceSnapshotService snapshots,
    IForecastEngine forecast,
    IRecommendationOrchestrator recommendations) :
    IRequestHandler<RefreshDashboardCommand, int>,
    IRequestHandler<GenerateForecastCommand, ForecastSnapshotResult>,
    IRequestHandler<RebuildSearchIndexCommand, int>
{
    public async Task<int> Handle(RefreshDashboardCommand request, CancellationToken ct)
    {
        await snapshots.RefreshOrganizationAsync(request.OrganizationId, Guid.NewGuid().ToString(), fullReconcile: true, ct);
        return await recommendations.GenerateAsync(request.OrganizationId, Guid.NewGuid().ToString(), ct);
    }

    public Task<ForecastSnapshotResult> Handle(GenerateForecastCommand request, CancellationToken ct)
        => forecast.GenerateAsync(request.OrganizationId, request.ForecastType, Guid.NewGuid().ToString(), ct);

    public async Task<int> Handle(RebuildSearchIndexCommand request, CancellationToken ct)
    {
        await snapshots.RefreshOrganizationAsync(request.OrganizationId, Guid.NewGuid().ToString(), ct: ct);
        return 1;
    }
}
