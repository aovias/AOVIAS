using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Authorization;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.DTO;
using EnterpriseDirectoryService.Application.Contracts;
using EnterpriseDirectoryService.Application.Queries;

namespace EnterpriseDirectoryService.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/directory")]
[Authorize]
public class DirectoryController(IMediator mediator) : ControllerBase
{
    private string TraceId => HttpContext.TraceIdentifier;

    [HttpGet("search")]
    [RequirePermission("Directory.Search.Read")]
    public async Task<ActionResult<ApiResponse<DirectorySearchResultDto>>> Search(
        [FromQuery] string? q, [FromQuery] Guid organizationId,
        [FromQuery] Guid? hospitalId, [FromQuery] Guid? departmentId,
        [FromQuery] string? status, [FromQuery] string? category, [FromQuery] string? skill, [FromQuery] string? certification,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] bool fuzzy = false, [FromQuery] bool phonetic = false,
        CancellationToken ct = default)
    {
        var query = new DirectoryQuery
        {
            Text = q ?? string.Empty,
            Page = page,
            PageSize = pageSize,
            Fuzzy = fuzzy,
            Phonetic = phonetic,
            Filters = new DirectoryQueryFilters
            {
                OrganizationId = organizationId,
                HospitalId = hospitalId,
                DepartmentId = departmentId,
                EmploymentStatus = status,
                ProfessionalCategory = category,
                Skill = skill,
                Certification = certification
            }
        };
        var data = await mediator.Send(new SearchDirectoryQuery(query), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, TraceId));
    }

    [HttpGet("profile/{id:guid}")]
    [RequirePermission("Directory.Profile.Read")]
    public async Task<ActionResult<ApiResponse<EnterpriseDirectoryProfileDto>>> Profile(Guid id, [FromQuery] Guid? organizationId, CancellationToken ct)
    {
        var data = await mediator.Send(new GetDirectoryProfileQuery(id, organizationId), ct);
        if (data is null) return NotFound(ApiResponseFactory.Failure<EnterpriseDirectoryProfileDto>(ApiResponseCode.RESOURCE_NOT_FOUND, TraceId));
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, TraceId));
    }

    [HttpGet("suggestions")]
    [RequirePermission("Directory.Search.Read")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DirectorySuggestionDto>>>> Suggestions([FromQuery] string prefix, [FromQuery] Guid organizationId, CancellationToken ct)
    {
        var data = await mediator.Send(new GetDirectorySuggestionsQuery(prefix, organizationId), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, TraceId));
    }

    [HttpGet("facets")]
    [RequirePermission("Directory.Search.Read")]
    public async Task<ActionResult<ApiResponse<IReadOnlyDictionary<string, IReadOnlyList<FacetValueDto>>>>> Facets([FromQuery] Guid organizationId, CancellationToken ct)
    {
        var data = await mediator.Send(new GetDirectoryFacetsQuery(organizationId), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, TraceId));
    }

    [HttpGet("popular")]
    [RequirePermission("Directory.Search.Read")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PopularSearchTermDto>>>> Popular([FromQuery] Guid organizationId, CancellationToken ct)
    {
        var data = await mediator.Send(new GetPopularSearchesQuery(organizationId), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, TraceId));
    }

    [HttpGet("recent")]
    [RequirePermission("Directory.History.Read")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SearchHistoryDto>>>> Recent([FromQuery] Guid organizationId, CancellationToken ct)
    {
        var data = await mediator.Send(new GetRecentSearchesQuery(organizationId), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, TraceId));
    }

    [HttpGet("history")]
    [RequirePermission("Directory.History.Read")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SearchHistoryDto>>>> History([FromQuery] Guid organizationId, CancellationToken ct)
    {
        var data = await mediator.Send(new GetSearchHistoryQuery(organizationId), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, TraceId));
    }

    [HttpGet("preferences")]
    [RequirePermission("Directory.Search.Read")]
    public async Task<ActionResult<ApiResponse<SearchPreferenceDto>>> GetPreferences(CancellationToken ct)
    {
        var data = await mediator.Send(new GetSearchPreferencesQuery(), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, TraceId));
    }

    [HttpPut("preferences")]
    [RequirePermission("Directory.Search.Read")]
    public async Task<ActionResult<ApiResponse<SearchPreferenceDto>>> UpdatePreferences([FromBody] UpdateSearchPreferenceRequest request, CancellationToken ct)
    {
        var data = await mediator.Send(new UpdateSearchPreferencesCommand(request), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, TraceId));
    }

    [HttpPost("saved-searches")]
    [RequirePermission("Directory.SavedSearch.Manage")]
    public async Task<ActionResult<ApiResponse<SavedSearchDto>>> CreateSavedSearch([FromBody] CreateSavedSearchRequest request, CancellationToken ct)
    {
        var data = await mediator.Send(new CreateSavedSearchCommand(request), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, TraceId));
    }

    [HttpGet("saved-searches")]
    [RequirePermission("Directory.SavedSearch.Manage")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SavedSearchDto>>>> ListSavedSearches([FromQuery] Guid organizationId, CancellationToken ct)
    {
        var data = await mediator.Send(new ListSavedSearchesQuery(organizationId), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, TraceId));
    }

    [HttpDelete("saved-searches/{id:guid}")]
    [RequirePermission("Directory.SavedSearch.Manage")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteSavedSearch(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeleteSavedSearchCommand(id), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, new { deleted = true }, TraceId));
    }

    [HttpPost("nl-search")]
    [RequirePermission("Directory.NLSearch.Use")]
    public async Task<ActionResult<ApiResponse<NaturalLanguageSearchResultDto>>> NaturalLanguageSearch([FromBody] NaturalLanguageSearchRequest request, CancellationToken ct)
    {
        var data = await mediator.Send(new NaturalLanguageSearchCommand(request), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, TraceId));
    }

    [HttpPost("rebuild")]
    [RequirePermission("Directory.Search.Read")]
    public async Task<ActionResult<ApiResponse<int>>> Rebuild([FromQuery] Guid organizationId, CancellationToken ct)
    {
        var count = await mediator.Send(new RebuildDirectoryIndexCommand(organizationId), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, count, TraceId));
    }
}
