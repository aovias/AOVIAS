using Gateway.Application.Queries;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Handlers;

public static class EnterpriseDirectoryProxyHandlers
{
    private static string SearchQs(SearchDirectoryQuery r)
    {
        var q = $"organizationId={r.OrganizationId}&page={r.Page}&pageSize={r.PageSize}&fuzzy={r.Fuzzy}&phonetic={r.Phonetic}";
        if (!string.IsNullOrWhiteSpace(r.Q)) q += $"&q={Uri.EscapeDataString(r.Q)}";
        if (r.HospitalId.HasValue) q += $"&hospitalId={r.HospitalId}";
        if (r.DepartmentId.HasValue) q += $"&departmentId={r.DepartmentId}";
        if (!string.IsNullOrWhiteSpace(r.Status)) q += $"&status={Uri.EscapeDataString(r.Status)}";
        if (!string.IsNullOrWhiteSpace(r.Category)) q += $"&category={Uri.EscapeDataString(r.Category)}";
        if (!string.IsNullOrWhiteSpace(r.Skill)) q += $"&skill={Uri.EscapeDataString(r.Skill)}";
        if (!string.IsNullOrWhiteSpace(r.Certification)) q += $"&certification={Uri.EscapeDataString(r.Certification)}";
        return q;
    }

    public sealed class SearchHandler(IProxyService proxy) : IRequestHandler<SearchDirectoryQuery, IActionResult>
    {
        public Task<IActionResult> Handle(SearchDirectoryQuery r, CancellationToken ct)
            => proxy.ProxyToEnterpriseDirectoryServiceAsync($"/api/v{r.ApiVersion}/directory/search?{SearchQs(r)}", HttpMethod.Get, cancellationToken: ct);
    }

    public sealed class ProfileHandler(IProxyService proxy) : IRequestHandler<GetDirectoryProfileQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetDirectoryProfileQuery r, CancellationToken ct)
            => proxy.ProxyToEnterpriseDirectoryServiceAsync($"/api/v{r.ApiVersion}/directory/profile/{r.Id}" + (r.OrganizationId.HasValue ? $"?organizationId={r.OrganizationId}" : ""), HttpMethod.Get, cancellationToken: ct);
    }

    public sealed class SuggestionsHandler(IProxyService proxy) : IRequestHandler<GetDirectorySuggestionsQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetDirectorySuggestionsQuery r, CancellationToken ct)
            => proxy.ProxyToEnterpriseDirectoryServiceAsync($"/api/v{r.ApiVersion}/directory/suggestions?prefix={Uri.EscapeDataString(r.Prefix)}&organizationId={r.OrganizationId}", HttpMethod.Get, cancellationToken: ct);
    }

    public sealed class FacetsHandler(IProxyService proxy) : IRequestHandler<GetDirectoryFacetsQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetDirectoryFacetsQuery r, CancellationToken ct)
            => proxy.ProxyToEnterpriseDirectoryServiceAsync($"/api/v{r.ApiVersion}/directory/facets?organizationId={r.OrganizationId}", HttpMethod.Get, cancellationToken: ct);
    }

    public sealed class PopularHandler(IProxyService proxy) : IRequestHandler<GetDirectoryPopularQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetDirectoryPopularQuery r, CancellationToken ct)
            => proxy.ProxyToEnterpriseDirectoryServiceAsync($"/api/v{r.ApiVersion}/directory/popular?organizationId={r.OrganizationId}", HttpMethod.Get, cancellationToken: ct);
    }

    public sealed class RecentHandler(IProxyService proxy) : IRequestHandler<GetDirectoryRecentQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetDirectoryRecentQuery r, CancellationToken ct)
            => proxy.ProxyToEnterpriseDirectoryServiceAsync($"/api/v{r.ApiVersion}/directory/recent?organizationId={r.OrganizationId}", HttpMethod.Get, cancellationToken: ct);
    }

    public sealed class HistoryHandler(IProxyService proxy) : IRequestHandler<GetDirectoryHistoryQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetDirectoryHistoryQuery r, CancellationToken ct)
            => proxy.ProxyToEnterpriseDirectoryServiceAsync($"/api/v{r.ApiVersion}/directory/history?organizationId={r.OrganizationId}", HttpMethod.Get, cancellationToken: ct);
    }

    public sealed class PreferencesGetHandler(IProxyService proxy) : IRequestHandler<GetDirectoryPreferencesQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetDirectoryPreferencesQuery r, CancellationToken ct)
            => proxy.ProxyToEnterpriseDirectoryServiceAsync($"/api/v{r.ApiVersion}/directory/preferences", HttpMethod.Get, cancellationToken: ct);
    }

    public sealed class PreferencesUpdateHandler(IProxyService proxy) : IRequestHandler<UpdateDirectoryPreferencesQuery, IActionResult>
    {
        public Task<IActionResult> Handle(UpdateDirectoryPreferencesQuery r, CancellationToken ct)
            => proxy.ProxyToEnterpriseDirectoryServiceAsync($"/api/v{r.ApiVersion}/directory/preferences", HttpMethod.Put, r.Body, cancellationToken: ct);
    }

    public sealed class CreateSavedSearchHandler(IProxyService proxy) : IRequestHandler<CreateDirectorySavedSearchQuery, IActionResult>
    {
        public Task<IActionResult> Handle(CreateDirectorySavedSearchQuery r, CancellationToken ct)
            => proxy.ProxyToEnterpriseDirectoryServiceAsync($"/api/v{r.ApiVersion}/directory/saved-searches", HttpMethod.Post, r.Body, cancellationToken: ct);
    }

    public sealed class ListSavedSearchesHandler(IProxyService proxy) : IRequestHandler<ListDirectorySavedSearchesQuery, IActionResult>
    {
        public Task<IActionResult> Handle(ListDirectorySavedSearchesQuery r, CancellationToken ct)
            => proxy.ProxyToEnterpriseDirectoryServiceAsync($"/api/v{r.ApiVersion}/directory/saved-searches?organizationId={r.OrganizationId}", HttpMethod.Get, cancellationToken: ct);
    }

    public sealed class DeleteSavedSearchHandler(IProxyService proxy) : IRequestHandler<DeleteDirectorySavedSearchQuery, IActionResult>
    {
        public Task<IActionResult> Handle(DeleteDirectorySavedSearchQuery r, CancellationToken ct)
            => proxy.ProxyToEnterpriseDirectoryServiceAsync($"/api/v{r.ApiVersion}/directory/saved-searches/{r.Id}", HttpMethod.Delete, cancellationToken: ct);
    }

    public sealed class NaturalLanguageHandler(IProxyService proxy) : IRequestHandler<DirectoryNaturalLanguageSearchQuery, IActionResult>
    {
        public Task<IActionResult> Handle(DirectoryNaturalLanguageSearchQuery r, CancellationToken ct)
            => proxy.ProxyToEnterpriseDirectoryServiceAsync($"/api/v{r.ApiVersion}/directory/nl-search", HttpMethod.Post, r.Body, cancellationToken: ct);
    }

    public sealed class RebuildHandler(IProxyService proxy) : IRequestHandler<RebuildDirectoryIndexQuery, IActionResult>
    {
        public Task<IActionResult> Handle(RebuildDirectoryIndexQuery r, CancellationToken ct)
            => proxy.ProxyToEnterpriseDirectoryServiceAsync($"/api/v{r.ApiVersion}/directory/rebuild?organizationId={r.OrganizationId}", HttpMethod.Post, cancellationToken: ct);
    }
}
