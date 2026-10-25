using Gateway.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Authorization;
using Shared.Infrastructure.Execution;

namespace Gateway.Controllers;

/// <summary>
/// Directory endpoints take <c>organizationId</c> from the query string. Permission alone does not
/// establish which tenant a caller may read or rebuild, so <see cref="ValidateTenantScopeAttribute"/>
/// pins that argument to the caller's authenticated organization (Wave 2.2A risk R-02).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/directory")]
[Authorize]
[ValidateTenantScope]
public class EnterpriseDirectoryController(IMediator mediator) : ControllerBase
{
    private string V() => HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

    [HttpGet("search")]
    [RequirePermission("Directory.Search.Read")]
    public Task<IActionResult> Search(
        [FromQuery] Guid organizationId, [FromQuery] string? q,
        [FromQuery] Guid? hospitalId, [FromQuery] Guid? departmentId,
        [FromQuery] string? status, [FromQuery] string? category, [FromQuery] string? skill, [FromQuery] string? certification,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] bool fuzzy = false, [FromQuery] bool phonetic = false,
        CancellationToken ct = default)
        => mediator.Send(new SearchDirectoryQuery(V(), organizationId, q, hospitalId, departmentId, status, category, skill, certification, page, pageSize, fuzzy, phonetic), ct);

    [HttpGet("profile/{id:guid}")]
    [RequirePermission("Directory.Profile.Read")]
    public Task<IActionResult> Profile(Guid id, [FromQuery] Guid? organizationId, CancellationToken ct)
        => mediator.Send(new GetDirectoryProfileQuery(V(), id, organizationId), ct);

    [HttpGet("suggestions")]
    [RequirePermission("Directory.Search.Read")]
    public Task<IActionResult> Suggestions([FromQuery] string prefix, [FromQuery] Guid organizationId, CancellationToken ct)
        => mediator.Send(new GetDirectorySuggestionsQuery(V(), prefix, organizationId), ct);

    [HttpGet("facets")]
    [RequirePermission("Directory.Search.Read")]
    public Task<IActionResult> Facets([FromQuery] Guid organizationId, CancellationToken ct)
        => mediator.Send(new GetDirectoryFacetsQuery(V(), organizationId), ct);

    [HttpGet("popular")]
    [RequirePermission("Directory.Search.Read")]
    public Task<IActionResult> Popular([FromQuery] Guid organizationId, CancellationToken ct)
        => mediator.Send(new GetDirectoryPopularQuery(V(), organizationId), ct);

    [HttpGet("recent")]
    [RequirePermission("Directory.History.Read")]
    public Task<IActionResult> Recent([FromQuery] Guid organizationId, CancellationToken ct)
        => mediator.Send(new GetDirectoryRecentQuery(V(), organizationId), ct);

    [HttpGet("history")]
    [RequirePermission("Directory.History.Read")]
    public Task<IActionResult> History([FromQuery] Guid organizationId, CancellationToken ct)
        => mediator.Send(new GetDirectoryHistoryQuery(V(), organizationId), ct);

    [HttpGet("preferences")]
    [RequirePermission("Directory.Search.Read")]
    public Task<IActionResult> GetPreferences(CancellationToken ct)
        => mediator.Send(new GetDirectoryPreferencesQuery(V()), ct);

    [HttpPut("preferences")]
    [RequirePermission("Directory.Search.Read")]
    public Task<IActionResult> UpdatePreferences([FromBody] object body, CancellationToken ct)
        => mediator.Send(new UpdateDirectoryPreferencesQuery(V(), body), ct);

    [HttpPost("saved-searches")]
    [RequirePermission("Directory.SavedSearch.Manage")]
    public Task<IActionResult> CreateSavedSearch([FromBody] object body, CancellationToken ct)
        => mediator.Send(new CreateDirectorySavedSearchQuery(V(), body), ct);

    [HttpGet("saved-searches")]
    [RequirePermission("Directory.SavedSearch.Manage")]
    public Task<IActionResult> ListSavedSearches([FromQuery] Guid organizationId, CancellationToken ct)
        => mediator.Send(new ListDirectorySavedSearchesQuery(V(), organizationId), ct);

    [HttpDelete("saved-searches/{id:guid}")]
    [RequirePermission("Directory.SavedSearch.Manage")]
    public Task<IActionResult> DeleteSavedSearch(Guid id, CancellationToken ct)
        => mediator.Send(new DeleteDirectorySavedSearchQuery(V(), id), ct);

    [HttpPost("nl-search")]
    [RequirePermission("Directory.NLSearch.Use")]
    public Task<IActionResult> NaturalLanguageSearch([FromBody] object body, CancellationToken ct)
        => mediator.Send(new DirectoryNaturalLanguageSearchQuery(V(), body), ct);

    [HttpPost("rebuild")]
    [RequirePermission("Directory.Search.Read")]
    public Task<IActionResult> Rebuild([FromQuery] Guid organizationId, CancellationToken ct)
        => mediator.Send(new RebuildDirectoryIndexQuery(V(), organizationId), ct);
}
