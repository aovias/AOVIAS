using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

public sealed record SearchDirectoryQuery(string ApiVersion, Guid OrganizationId, string? Q, Guid? HospitalId, Guid? DepartmentId, string? Status, string? Category, string? Skill, string? Certification, int Page, int PageSize, bool Fuzzy, bool Phonetic) : IRequest<IActionResult>;
public sealed record GetDirectoryProfileQuery(string ApiVersion, Guid Id, Guid? OrganizationId) : IRequest<IActionResult>;
public sealed record GetDirectorySuggestionsQuery(string ApiVersion, string Prefix, Guid OrganizationId) : IRequest<IActionResult>;
public sealed record GetDirectoryFacetsQuery(string ApiVersion, Guid OrganizationId) : IRequest<IActionResult>;
public sealed record GetDirectoryPopularQuery(string ApiVersion, Guid OrganizationId) : IRequest<IActionResult>;
public sealed record GetDirectoryRecentQuery(string ApiVersion, Guid OrganizationId) : IRequest<IActionResult>;
public sealed record GetDirectoryHistoryQuery(string ApiVersion, Guid OrganizationId) : IRequest<IActionResult>;
public sealed record GetDirectoryPreferencesQuery(string ApiVersion) : IRequest<IActionResult>;
public sealed record UpdateDirectoryPreferencesQuery(string ApiVersion, object Body) : IRequest<IActionResult>;
public sealed record CreateDirectorySavedSearchQuery(string ApiVersion, object Body) : IRequest<IActionResult>;
public sealed record ListDirectorySavedSearchesQuery(string ApiVersion, Guid OrganizationId) : IRequest<IActionResult>;
public sealed record DeleteDirectorySavedSearchQuery(string ApiVersion, Guid Id) : IRequest<IActionResult>;
public sealed record DirectoryNaturalLanguageSearchQuery(string ApiVersion, object Body) : IRequest<IActionResult>;
public sealed record RebuildDirectoryIndexQuery(string ApiVersion, Guid OrganizationId) : IRequest<IActionResult>;
