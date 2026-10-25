using EnterpriseDirectoryService.Application.Contracts;
using MediatR;

namespace EnterpriseDirectoryService.Application.Queries;

public sealed record SearchDirectoryQuery(DirectoryQuery Query) : IRequest<DirectorySearchResultDto>;
public sealed record GetDirectoryProfileQuery(Guid Id, Guid? OrganizationId) : IRequest<EnterpriseDirectoryProfileDto?>;
public sealed record GetDirectorySuggestionsQuery(string Prefix, Guid OrganizationId) : IRequest<IReadOnlyList<DirectorySuggestionDto>>;
public sealed record GetDirectoryFacetsQuery(Guid OrganizationId) : IRequest<IReadOnlyDictionary<string, IReadOnlyList<FacetValueDto>>>;
public sealed record GetPopularSearchesQuery(Guid OrganizationId) : IRequest<IReadOnlyList<PopularSearchTermDto>>;
public sealed record GetRecentSearchesQuery(Guid OrganizationId) : IRequest<IReadOnlyList<SearchHistoryDto>>;
public sealed record GetSearchHistoryQuery(Guid OrganizationId) : IRequest<IReadOnlyList<SearchHistoryDto>>;
public sealed record GetSearchPreferencesQuery : IRequest<SearchPreferenceDto>;
public sealed record UpdateSearchPreferencesCommand(UpdateSearchPreferenceRequest Request) : IRequest<SearchPreferenceDto>;
public sealed record CreateSavedSearchCommand(CreateSavedSearchRequest Request) : IRequest<SavedSearchDto>;
public sealed record ListSavedSearchesQuery(Guid OrganizationId) : IRequest<IReadOnlyList<SavedSearchDto>>;
public sealed record DeleteSavedSearchCommand(Guid Id) : IRequest<Unit>;
public sealed record NaturalLanguageSearchCommand(NaturalLanguageSearchRequest Request) : IRequest<NaturalLanguageSearchResultDto>;
public sealed record RebuildDirectoryIndexCommand(Guid OrganizationId) : IRequest<int>;
