using System.Security.Claims;
using EnterpriseDirectoryService.Application.Contracts;
using EnterpriseDirectoryService.Application.Directory.Indexing;
using EnterpriseDirectoryService.Application.Directory.Platform;
using EnterpriseDirectoryService.Application.Directory.SavedSearch;
using EnterpriseDirectoryService.Application.Directory.Security;
using EnterpriseDirectoryService.Application.Queries;
using MediatR;

namespace EnterpriseDirectoryService.Application.Handlers;

public static class DirectoryHandlers
{
    private static Guid UserId(ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value, out var id) ? id : Guid.Empty;

    public sealed class SearchHandler(DirectoryOrchestrationService orchestration, IHttpContextAccessor http) : IRequestHandler<SearchDirectoryQuery, DirectorySearchResultDto>
    {
        public Task<DirectorySearchResultDto> Handle(SearchDirectoryQuery request, CancellationToken ct)
            => orchestration.SearchAsync(http.HttpContext!.User, request.Query, ct);
    }

    public sealed class ProfileHandler(DirectoryOrchestrationService orchestration, IHttpContextAccessor http) : IRequestHandler<GetDirectoryProfileQuery, EnterpriseDirectoryProfileDto?>
    {
        public Task<EnterpriseDirectoryProfileDto?> Handle(GetDirectoryProfileQuery request, CancellationToken ct)
            => orchestration.GetProfileAsync(http.HttpContext!.User, request.Id, request.OrganizationId, ct);
    }

    public sealed class SuggestionsHandler(IDirectorySearchEngine search, IDirectoryAuthorizationService auth, IHttpContextAccessor http) : IRequestHandler<GetDirectorySuggestionsQuery, IReadOnlyList<DirectorySuggestionDto>>
    {
        public async Task<IReadOnlyList<DirectorySuggestionDto>> Handle(GetDirectorySuggestionsQuery request, CancellationToken ct)
        {
            var ctx = await auth.BuildContextAsync(http.HttpContext!.User, request.OrganizationId, ct);
            return await search.SuggestAsync(request.Prefix, ctx, request.OrganizationId, ct);
        }
    }

    public sealed class FacetsHandler(IDirectorySearchEngine search, IDirectoryAuthorizationService auth, IHttpContextAccessor http) : IRequestHandler<GetDirectoryFacetsQuery, IReadOnlyDictionary<string, IReadOnlyList<FacetValueDto>>>
    {
        public async Task<IReadOnlyDictionary<string, IReadOnlyList<FacetValueDto>>> Handle(GetDirectoryFacetsQuery request, CancellationToken ct)
        {
            var ctx = await auth.BuildContextAsync(http.HttpContext!.User, request.OrganizationId, ct);
            return await search.GetFacetsAsync(ctx, request.OrganizationId, ct);
        }
    }

    public sealed class PopularHandler(ISearchHistoryService history) : IRequestHandler<GetPopularSearchesQuery, IReadOnlyList<PopularSearchTermDto>>
    {
        public Task<IReadOnlyList<PopularSearchTermDto>> Handle(GetPopularSearchesQuery request, CancellationToken ct)
            => history.PopularAsync(request.OrganizationId, ct: ct);
    }

    public sealed class RecentHandler(ISearchHistoryService history, IHttpContextAccessor http) : IRequestHandler<GetRecentSearchesQuery, IReadOnlyList<SearchHistoryDto>>
    {
        public Task<IReadOnlyList<SearchHistoryDto>> Handle(GetRecentSearchesQuery request, CancellationToken ct)
            => history.RecentAsync(UserId(http.HttpContext!.User), request.OrganizationId, ct: ct);
    }

    public sealed class HistoryHandler(ISearchHistoryService history, IHttpContextAccessor http) : IRequestHandler<GetSearchHistoryQuery, IReadOnlyList<SearchHistoryDto>>
    {
        public Task<IReadOnlyList<SearchHistoryDto>> Handle(GetSearchHistoryQuery request, CancellationToken ct)
            => history.ListAsync(UserId(http.HttpContext!.User), request.OrganizationId, ct: ct);
    }

    public sealed class PreferencesGetHandler(ISearchPreferenceService prefs, IHttpContextAccessor http) : IRequestHandler<GetSearchPreferencesQuery, SearchPreferenceDto>
    {
        public Task<SearchPreferenceDto> Handle(GetSearchPreferencesQuery request, CancellationToken ct)
            => prefs.GetAsync(UserId(http.HttpContext!.User), ct);
    }

    public sealed class PreferencesUpdateHandler(ISearchPreferenceService prefs, IHttpContextAccessor http) : IRequestHandler<UpdateSearchPreferencesCommand, SearchPreferenceDto>
    {
        public Task<SearchPreferenceDto> Handle(UpdateSearchPreferencesCommand request, CancellationToken ct)
            => prefs.UpdateAsync(UserId(http.HttpContext!.User), request.Request, ct);
    }

    public sealed class CreateSavedSearchHandler(ISavedSearchService saved, IHttpContextAccessor http) : IRequestHandler<CreateSavedSearchCommand, SavedSearchDto>
    {
        public Task<SavedSearchDto> Handle(CreateSavedSearchCommand request, CancellationToken ct)
            => saved.CreateAsync(UserId(http.HttpContext!.User), request.Request, ct);
    }

    public sealed class ListSavedSearchesHandler(ISavedSearchService saved, IHttpContextAccessor http) : IRequestHandler<ListSavedSearchesQuery, IReadOnlyList<SavedSearchDto>>
    {
        public Task<IReadOnlyList<SavedSearchDto>> Handle(ListSavedSearchesQuery request, CancellationToken ct)
            => saved.ListAsync(UserId(http.HttpContext!.User), request.OrganizationId, ct);
    }

    public sealed class DeleteSavedSearchHandler(ISavedSearchService saved, IHttpContextAccessor http) : IRequestHandler<DeleteSavedSearchCommand, Unit>
    {
        public async Task<Unit> Handle(DeleteSavedSearchCommand request, CancellationToken ct)
        {
            await saved.DeleteAsync(UserId(http.HttpContext!.User), request.Id, ct);
            return Unit.Value;
        }
    }

    public sealed class NaturalLanguageHandler(DirectoryOrchestrationService orchestration, IHttpContextAccessor http) : IRequestHandler<NaturalLanguageSearchCommand, NaturalLanguageSearchResultDto>
    {
        public Task<NaturalLanguageSearchResultDto> Handle(NaturalLanguageSearchCommand request, CancellationToken ct)
            => orchestration.NaturalLanguageSearchAsync(http.HttpContext!.User, request.Request, ct);
    }

    public sealed class RebuildIndexHandler(IDirectoryIndexProjector projector) : IRequestHandler<RebuildDirectoryIndexCommand, int>
    {
        public Task<int> Handle(RebuildDirectoryIndexCommand request, CancellationToken ct)
            => projector.RebuildOrganizationAsync(request.OrganizationId, Guid.NewGuid().ToString(), ct);
    }
}
