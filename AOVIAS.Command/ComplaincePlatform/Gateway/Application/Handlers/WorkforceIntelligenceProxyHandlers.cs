using Gateway.Application.Queries;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Handlers;

public static class WorkforceIntelligenceProxyHandlers
{
    private static string Q(Guid orgId, Guid? hospitalId, Guid? departmentId, DateTime? asOf)
    {
        var q = $"organizationId={orgId}";
        if (hospitalId.HasValue) q += $"&hospitalId={hospitalId}";
        if (departmentId.HasValue) q += $"&departmentId={departmentId}";
        if (asOf.HasValue) q += $"&asOf={asOf:O}";
        return q;
    }

    public sealed class DashboardHandler(IProxyService proxy) : IRequestHandler<GetIntelligenceDashboardQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetIntelligenceDashboardQuery r, CancellationToken ct)
            => proxy.ProxyToWorkforceIntelligenceServiceAsync($"/api/v{r.ApiVersion}/intelligence/dashboard?{Q(r.OrganizationId, r.HospitalId, r.DepartmentId, r.AsOf)}", HttpMethod.Get, cancellationToken: ct);
    }

    public sealed class HeadcountHandler(IProxyService proxy) : IRequestHandler<GetIntelligenceHeadcountQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetIntelligenceHeadcountQuery r, CancellationToken ct)
            => proxy.ProxyToWorkforceIntelligenceServiceAsync($"/api/v{r.ApiVersion}/intelligence/headcount?{Q(r.OrganizationId, r.HospitalId, r.DepartmentId, r.AsOf)}", HttpMethod.Get, cancellationToken: ct);
    }

    public sealed class SkillsHandler(IProxyService proxy) : IRequestHandler<GetIntelligenceSkillsQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetIntelligenceSkillsQuery r, CancellationToken ct)
            => proxy.ProxyToWorkforceIntelligenceServiceAsync($"/api/v{r.ApiVersion}/intelligence/skills?{Q(r.OrganizationId, r.HospitalId, r.DepartmentId, r.AsOf)}", HttpMethod.Get, cancellationToken: ct);
    }

    public sealed class CompetenciesHandler(IProxyService proxy) : IRequestHandler<GetIntelligenceCompetenciesQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetIntelligenceCompetenciesQuery r, CancellationToken ct)
            => proxy.ProxyToWorkforceIntelligenceServiceAsync($"/api/v{r.ApiVersion}/intelligence/competencies?{Q(r.OrganizationId, r.HospitalId, r.DepartmentId, r.AsOf)}", HttpMethod.Get, cancellationToken: ct);
    }

    public sealed class TrainingHandler(IProxyService proxy) : IRequestHandler<GetIntelligenceTrainingQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetIntelligenceTrainingQuery r, CancellationToken ct)
            => proxy.ProxyToWorkforceIntelligenceServiceAsync($"/api/v{r.ApiVersion}/intelligence/training?{Q(r.OrganizationId, r.HospitalId, r.DepartmentId, r.AsOf)}", HttpMethod.Get, cancellationToken: ct);
    }

    public sealed class LicensesHandler(IProxyService proxy) : IRequestHandler<GetIntelligenceLicensesQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetIntelligenceLicensesQuery r, CancellationToken ct)
            => proxy.ProxyToWorkforceIntelligenceServiceAsync($"/api/v{r.ApiVersion}/intelligence/licenses?{Q(r.OrganizationId, r.HospitalId, r.DepartmentId, r.AsOf)}", HttpMethod.Get, cancellationToken: ct);
    }

    public sealed class CertificationsHandler(IProxyService proxy) : IRequestHandler<GetIntelligenceCertificationsQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetIntelligenceCertificationsQuery r, CancellationToken ct)
            => proxy.ProxyToWorkforceIntelligenceServiceAsync($"/api/v{r.ApiVersion}/intelligence/certifications?{Q(r.OrganizationId, r.HospitalId, r.DepartmentId, r.AsOf)}", HttpMethod.Get, cancellationToken: ct);
    }

    public sealed class ForecastHandler(IProxyService proxy) : IRequestHandler<GetIntelligenceForecastQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetIntelligenceForecastQuery r, CancellationToken ct)
            => proxy.ProxyToWorkforceIntelligenceServiceAsync($"/api/v{r.ApiVersion}/intelligence/forecast?organizationId={r.OrganizationId}&forecastType={Uri.EscapeDataString(r.ForecastType)}", HttpMethod.Get, cancellationToken: ct);
    }

    public sealed class RecommendationsHandler(IProxyService proxy) : IRequestHandler<GetIntelligenceRecommendationsQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetIntelligenceRecommendationsQuery r, CancellationToken ct)
            => proxy.ProxyToWorkforceIntelligenceServiceAsync($"/api/v{r.ApiVersion}/intelligence/recommendations?organizationId={r.OrganizationId}", HttpMethod.Get, cancellationToken: ct);
    }

    public sealed class SearchHandler(IProxyService proxy) : IRequestHandler<SearchIntelligenceWorkforceQuery, IActionResult>
    {
        public Task<IActionResult> Handle(SearchIntelligenceWorkforceQuery r, CancellationToken ct)
            => proxy.ProxyToWorkforceIntelligenceServiceAsync($"/api/v{r.ApiVersion}/intelligence/search?organizationId={r.OrganizationId}&query={Uri.EscapeDataString(r.Query)}&page={r.Page}&pageSize={r.PageSize}", HttpMethod.Get, cancellationToken: ct);
    }

    public sealed class OrganizationHandler(IProxyService proxy) : IRequestHandler<GetIntelligenceOrganizationQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetIntelligenceOrganizationQuery r, CancellationToken ct)
            => proxy.ProxyToWorkforceIntelligenceServiceAsync($"/api/v{r.ApiVersion}/intelligence/organization?organizationId={r.OrganizationId}" + (r.AsOf.HasValue ? $"&asOf={r.AsOf:O}" : ""), HttpMethod.Get, cancellationToken: ct);
    }

    public sealed class ResponsibilitiesHandler(IProxyService proxy) : IRequestHandler<GetIntelligenceResponsibilitiesQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetIntelligenceResponsibilitiesQuery r, CancellationToken ct)
            => proxy.ProxyToWorkforceIntelligenceServiceAsync($"/api/v{r.ApiVersion}/intelligence/responsibilities?organizationId={r.OrganizationId}" + (r.AsOf.HasValue ? $"&asOf={r.AsOf:O}" : ""), HttpMethod.Get, cancellationToken: ct);
    }

    public sealed class CommitteesHandler(IProxyService proxy) : IRequestHandler<GetIntelligenceCommitteesQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetIntelligenceCommitteesQuery r, CancellationToken ct)
            => proxy.ProxyToWorkforceIntelligenceServiceAsync($"/api/v{r.ApiVersion}/intelligence/committees?organizationId={r.OrganizationId}", HttpMethod.Get, cancellationToken: ct);
    }

    public sealed class AvailabilityHandler(IProxyService proxy) : IRequestHandler<GetIntelligenceAvailabilityQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetIntelligenceAvailabilityQuery r, CancellationToken ct)
            => proxy.ProxyToWorkforceIntelligenceServiceAsync($"/api/v{r.ApiVersion}/intelligence/availability?organizationId={r.OrganizationId}" + (r.HospitalId.HasValue ? $"&hospitalId={r.HospitalId}" : ""), HttpMethod.Get, cancellationToken: ct);
    }

    public sealed class RefreshHandler(IProxyService proxy) : IRequestHandler<RefreshIntelligenceDashboardQuery, IActionResult>
    {
        public Task<IActionResult> Handle(RefreshIntelligenceDashboardQuery r, CancellationToken ct)
            => proxy.ProxyToWorkforceIntelligenceServiceAsync($"/api/v{r.ApiVersion}/intelligence/refresh?organizationId={r.OrganizationId}", HttpMethod.Post, cancellationToken: ct);
    }
}
