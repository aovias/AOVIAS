using System.Text.Json;
using EnterpriseDirectoryService.Application.Contracts;
using EnterpriseDirectoryService.Application.Directory.NaturalLanguage;
using EnterpriseDirectoryService.Application.Directory.Platform;
using EnterpriseDirectoryService.Application.Directory.Profiles;
using EnterpriseDirectoryService.Application.Directory.Security;
using EnterpriseDirectoryService.Domain.ReadModels;
using EnterpriseDirectoryService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseDirectoryService.Application.Directory.SavedSearch;

public interface ISavedSearchService
{
    Task<SavedSearchDto> CreateAsync(Guid userId, CreateSavedSearchRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<SavedSearchDto>> ListAsync(Guid userId, Guid organizationId, CancellationToken ct = default);
    Task DeleteAsync(Guid userId, Guid id, CancellationToken ct = default);
}

public interface ISearchHistoryService
{
    Task RecordAsync(Guid userId, Guid organizationId, string terms, DirectoryQueryFilters filters, int resultCount, long executionMs, CancellationToken ct = default);
    Task<IReadOnlyList<SearchHistoryDto>> ListAsync(Guid userId, Guid organizationId, int take = 50, CancellationToken ct = default);
    Task<IReadOnlyList<PopularSearchTermDto>> PopularAsync(Guid organizationId, int take = 10, CancellationToken ct = default);
    Task<IReadOnlyList<SearchHistoryDto>> RecentAsync(Guid userId, Guid organizationId, int take = 10, CancellationToken ct = default);
}

public interface ISearchPreferenceService
{
    Task<SearchPreferenceDto> GetAsync(Guid userId, CancellationToken ct = default);
    Task<SearchPreferenceDto> UpdateAsync(Guid userId, UpdateSearchPreferenceRequest request, CancellationToken ct = default);
}

public sealed class SavedSearchService(EnterpriseDirectoryDbContext db) : ISavedSearchService
{
    private static readonly JsonSerializerOptions JsonOpts = new();

    public async Task<SavedSearchDto> CreateAsync(Guid userId, CreateSavedSearchRequest request, CancellationToken ct = default)
    {
        var entity = new Domain.ReadModels.SavedSearch
        {
            Id = Guid.NewGuid(),
            OwnerUserId = userId,
            OrganizationId = request.OrganizationId,
            Name = request.Name,
            QueryJson = JsonSerializer.Serialize(request.Query, JsonOpts),
            Visibility = request.Visibility,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.SavedSearches.Add(entity);
        await db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<IReadOnlyList<SavedSearchDto>> ListAsync(Guid userId, Guid organizationId, CancellationToken ct = default)
    {
        var items = await db.SavedSearches.AsNoTracking()
            .Where(s => s.OrganizationId == organizationId && (s.OwnerUserId == userId || s.Visibility != "Private"))
            .OrderByDescending(s => s.CreatedAtUtc)
            .ToListAsync(ct);
        return items.Select(Map).ToList();
    }

    public async Task DeleteAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var item = await db.SavedSearches.FirstOrDefaultAsync(s => s.Id == id && s.OwnerUserId == userId, ct);
        if (item is null) return;
        db.SavedSearches.Remove(item);
        await db.SaveChangesAsync(ct);
    }

    private static SavedSearchDto Map(Domain.ReadModels.SavedSearch s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        Query = JsonSerializer.Deserialize<DirectoryQuery>(s.QueryJson, JsonOpts) ?? new DirectoryQuery(),
        Visibility = s.Visibility,
        IsScheduled = s.IsScheduled,
        IsAiGenerated = s.IsAiGenerated,
        CreatedAtUtc = s.CreatedAtUtc
    };
}

public sealed class SearchHistoryService(EnterpriseDirectoryDbContext db) : ISearchHistoryService
{
    private static readonly JsonSerializerOptions JsonOpts = new();

    public async Task RecordAsync(Guid userId, Guid organizationId, string terms, DirectoryQueryFilters filters, int resultCount, long executionMs, CancellationToken ct = default)
    {
        db.SearchHistories.Add(new SearchHistory
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OrganizationId = organizationId,
            SearchTerms = terms,
            FiltersJson = JsonSerializer.Serialize(filters, JsonOpts),
            ResultCount = resultCount,
            ExecutionMs = executionMs,
            SearchedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<SearchHistoryDto>> ListAsync(Guid userId, Guid organizationId, int take = 50, CancellationToken ct = default)
    {
        var items = await db.SearchHistories.AsNoTracking()
            .Where(h => h.UserId == userId && h.OrganizationId == organizationId)
            .OrderByDescending(h => h.SearchedAtUtc)
            .Take(take)
            .ToListAsync(ct);
        return items.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<PopularSearchTermDto>> PopularAsync(Guid organizationId, int take = 10, CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.AddDays(-30);
        return await db.SearchHistories.AsNoTracking()
            .Where(h => h.OrganizationId == organizationId && h.SearchedAtUtc >= since && h.SearchTerms != "")
            .GroupBy(h => h.SearchTerms)
            .Select(g => new PopularSearchTermDto { Term = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(take)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<SearchHistoryDto>> RecentAsync(Guid userId, Guid organizationId, int take = 10, CancellationToken ct = default)
        => await ListAsync(userId, organizationId, take, ct);

    private static SearchHistoryDto Map(SearchHistory h) => new()
    {
        Id = h.Id,
        SearchTerms = h.SearchTerms,
        FiltersJson = h.FiltersJson,
        ResultCount = h.ResultCount,
        ExecutionMs = h.ExecutionMs,
        SearchedAtUtc = h.SearchedAtUtc
    };
}

public sealed class SearchPreferenceService(EnterpriseDirectoryDbContext db) : ISearchPreferenceService
{
    private static readonly JsonSerializerOptions JsonOpts = new();

    public async Task<SearchPreferenceDto> GetAsync(Guid userId, CancellationToken ct = default)
    {
        var pref = await db.SearchPreferences.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId, ct);
        return pref is null
            ? new SearchPreferenceDto()
            : new SearchPreferenceDto
            {
                DefaultOrganizationId = pref.DefaultOrganizationId,
                NaturalLanguageEnabled = pref.NaturalLanguageEnabled,
                PinnedResults = string.IsNullOrWhiteSpace(pref.PinnedResultsJson)
                    ? Array.Empty<string>()
                    : JsonSerializer.Deserialize<List<string>>(pref.PinnedResultsJson, JsonOpts) ?? []
            };
    }

    public async Task<SearchPreferenceDto> UpdateAsync(Guid userId, UpdateSearchPreferenceRequest request, CancellationToken ct = default)
    {
        var pref = await db.SearchPreferences.FirstOrDefaultAsync(p => p.UserId == userId, ct);
        if (pref is null)
        {
            pref = new SearchPreference { UserId = userId };
            db.SearchPreferences.Add(pref);
        }
        pref.DefaultOrganizationId = request.DefaultOrganizationId;
        pref.NaturalLanguageEnabled = request.NaturalLanguageEnabled;
        pref.PinnedResultsJson = request.PinnedResults is null ? null : JsonSerializer.Serialize(request.PinnedResults, JsonOpts);
        pref.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return await GetAsync(userId, ct);
    }
}

public sealed class DirectoryOrchestrationService(
    IDirectorySearchEngine searchEngine,
    IDirectoryAuthorizationService authorization,
    ISearchHistoryService history,
    INaturalLanguageQueryTranslator nlTranslator,
    IDirectoryProfileComposer profileComposer)
{
    public async Task<DirectorySearchResultDto> SearchAsync(System.Security.Claims.ClaimsPrincipal user, DirectoryQuery query, CancellationToken ct)
    {
        var auth = await authorization.BuildContextAsync(user, query.Filters.OrganizationId, ct);
        var result = await searchEngine.SearchAsync(query, auth, ct);
        var userId = Guid.TryParse(user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var id) ? id : Guid.Empty;
        if (userId != Guid.Empty && query.Filters.OrganizationId.HasValue)
            await history.RecordAsync(userId, query.Filters.OrganizationId.Value, query.Text, query.Filters, result.TotalCount, result.ExecutionMs, ct);
        return result;
    }

    public async Task<NaturalLanguageSearchResultDto> NaturalLanguageSearchAsync(System.Security.Claims.ClaimsPrincipal user, NaturalLanguageSearchRequest request, CancellationToken ct)
    {
        await authorization.EnsureSearchAllowedAsync(user, request.OrganizationId, ct);
        var translated = await nlTranslator.TranslateAsync(request.Query, request.OrganizationId, ct);
        var results = await SearchAsync(user, translated, ct);
        return new NaturalLanguageSearchResultDto { Results = results, TranslatedQuery = translated };
    }

    public async Task<EnterpriseDirectoryProfileDto?> GetProfileAsync(System.Security.Claims.ClaimsPrincipal user, Guid id, Guid? organizationId, CancellationToken ct)
    {
        await authorization.EnsureProfileAllowedAsync(user, organizationId, ct);
        var auth = await authorization.BuildContextAsync(user, organizationId, ct);
        return await profileComposer.ComposeAsync(id, auth, ct);
    }
}
