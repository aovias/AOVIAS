using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkforceService.Application.Commands;
using WorkforceService.Application.DTOs;
using WorkforceService.Application.Queries;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.DTO;
using System.Security.Claims;

namespace WorkforceService.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/employment")]
[Authorize]
public class EmploymentController : ControllerBase
{
    private readonly IMediator _mediator;

    public EmploymentController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResultDto<EmploymentListItemDto>>>> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] Guid? organizationId = null, [FromQuery] Guid? hospitalId = null, [FromQuery] Guid? statusValueId = null,
        CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var data = await _mediator.Send(new ListEmploymentsQuery(page, pageSize, organizationId, hospitalId, statusValueId), cancellationToken);
        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId);
        return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, response);
    }

    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<PagedResultDto<EmploymentListItemDto>>>> Search(
        [FromQuery(Name = "q")] string query, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var data = await _mediator.Send(new SearchEmploymentsQuery(query, page, pageSize), cancellationToken);
        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId);
        return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, response);
    }

    [HttpGet("org-chart")]
    public async Task<ActionResult<ApiResponse<OrgChartNodeDto>>> OrgChart(
        [FromQuery] Guid organizationId, [FromQuery] Guid? hospitalId = null, [FromQuery] Guid? rootEmploymentId = null,
        CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var data = await _mediator.Send(new GetOrgChartQuery(organizationId, hospitalId, rootEmploymentId), cancellationToken);
        if (data == null)
        {
            var failure = ApiResponseFactory.Failure<OrgChartNodeDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND].HttpStatus, failure);
        }
        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId);
        return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, response);
    }

    [HttpGet("manager/{managerEmploymentId:guid}/team")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<EmploymentListItemDto>>>> ManagerTeam(Guid managerEmploymentId, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var data = await _mediator.Send(new GetManagerTeamQuery(managerEmploymentId), cancellationToken);
        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId);
        return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, response);
    }

    [HttpGet("positions")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PositionDto>>>> Positions([FromQuery] Guid organizationId, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var data = await _mediator.Send(new GetPositionsQuery(organizationId), cancellationToken);
        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId);
        return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, response);
    }

    [HttpGet("lookups/{category}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LookupValueDto>>>> Lookups(string category, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var data = await _mediator.Send(new GetWorkforceLookupsQuery(category), cancellationToken);
        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId);
        return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<EmploymentDto>>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var employment = await _mediator.Send(new GetEmploymentByIdQuery(id), cancellationToken);
        if (employment == null)
        {
            var failure = ApiResponseFactory.Failure<EmploymentDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND].HttpStatus, failure);
        }
        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, employment, traceId);
        return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, response);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<EmploymentDto>>> Create([FromBody] CreateEmploymentRequest request, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        request.CreatedByUserId ??= ActorUserId();
        try
        {
            var result = await _mediator.Send(new CreateEmploymentCommand(request), cancellationToken);
            var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, result, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, response);
        }
        catch (InvalidOperationException ex)
        {
            var code = ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase) ? ApiResponseCode.RESOURCE_CONFLICT : ApiResponseCode.VALIDATION_FAILED;
            var failure = ApiResponseFactory.Failure<EmploymentDto>(code, traceId);
            return StatusCode(ApiResponseCodes.Registry[code].HttpStatus, failure);
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<EmploymentDto>>> Update(Guid id, [FromBody] UpdateEmploymentRequest request, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        request.CreatedByUserId ??= ActorUserId();
        var employment = await _mediator.Send(new UpdateEmploymentCommand(id, request), cancellationToken);
        if (employment == null)
        {
            var failure = ApiResponseFactory.Failure<EmploymentDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND].HttpStatus, failure);
        }
        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, employment, traceId);
        return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, response);
    }

    [HttpPatch("{id:guid}")]
    public Task<ActionResult<ApiResponse<EmploymentDto>>> Patch(Guid id, [FromBody] UpdateEmploymentRequest request, CancellationToken cancellationToken = default)
        => Update(id, request, cancellationToken);

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var deleted = await _mediator.Send(new DeleteEmploymentCommand(id, ActorUserId()), cancellationToken);
        if (!deleted)
        {
            var failure = ApiResponseFactory.Failure<object>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND].HttpStatus, failure);
        }
        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, (object)new { id }, traceId);
        return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, response);
    }

    [HttpPost("{id:guid}/assign-position")]
    public async Task<ActionResult<ApiResponse<PositionAssignmentDto>>> AssignPosition(Guid id, [FromBody] AssignPositionRequest request, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        request.ActorUserId ??= ActorUserId();
        try
        {
            var result = await _mediator.Send(new AssignPositionCommand(id, request), cancellationToken);
            if (result == null) return NotFoundResponse<PositionAssignmentDto>(traceId);
            var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, result, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, response);
        }
        catch (InvalidOperationException)
        {
            return ConflictResponse<PositionAssignmentDto>(traceId);
        }
    }

    [HttpPost("{id:guid}/assign-department")]
    public async Task<ActionResult<ApiResponse<DepartmentAssignmentDto>>> AssignDepartment(Guid id, [FromBody] AssignDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        request.ActorUserId ??= ActorUserId();
        var result = await _mediator.Send(new AssignDepartmentCommand(id, request), cancellationToken);
        if (result == null) return NotFoundResponse<DepartmentAssignmentDto>(traceId);
        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, result, traceId);
        return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, response);
    }

    [HttpPost("{id:guid}/assign-manager")]
    public async Task<ActionResult<ApiResponse<ReportingRelationshipDto>>> AssignManager(Guid id, [FromBody] AssignManagerRequest request, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        request.ActorUserId ??= ActorUserId();
        try
        {
            var result = await _mediator.Send(new AssignManagerCommand(id, request), cancellationToken);
            if (result == null) return NotFoundResponse<ReportingRelationshipDto>(traceId);
            var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, result, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, response);
        }
        catch (InvalidOperationException)
        {
            return ConflictResponse<ReportingRelationshipDto>(traceId);
        }
    }

    [HttpPost("{id:guid}/transfer")]
    public async Task<ActionResult<ApiResponse<EmploymentDto>>> Transfer(Guid id, [FromBody] TransferEmploymentRequest request, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        request.ActorUserId ??= ActorUserId();
        var result = await _mediator.Send(new TransferEmploymentCommand(id, request), cancellationToken);
        if (result == null) return NotFoundResponse<EmploymentDto>(traceId);
        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, result, traceId);
        return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, response);
    }

    [HttpPost("{id:guid}/promote")]
    public async Task<ActionResult<ApiResponse<EmploymentDto>>> Promote(Guid id, [FromBody] PromoteEmploymentRequest request, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        request.ActorUserId ??= ActorUserId();
        var result = await _mediator.Send(new PromoteEmploymentCommand(id, request), cancellationToken);
        if (result == null) return NotFoundResponse<EmploymentDto>(traceId);
        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, result, traceId);
        return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, response);
    }

    [HttpGet("{id:guid}/history")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<EmploymentHistoryDto>>>> History(Guid id, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var data = await _mediator.Send(new GetEmploymentHistoryQuery(id), cancellationToken);
        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId);
        return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, response);
    }

    [HttpGet("{id:guid}/timeline")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WorkforceTimelineDto>>>> Timeline(Guid id, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var data = await _mediator.Send(new GetEmploymentTimelineQuery(id), cancellationToken);
        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId);
        return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, response);
    }

    private Guid? ActorUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    private ActionResult<ApiResponse<T>> NotFoundResponse<T>(string traceId)
    {
        var failure = ApiResponseFactory.Failure<T>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId);
        return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND].HttpStatus, failure);
    }

    private ActionResult<ApiResponse<T>> ConflictResponse<T>(string traceId)
    {
        var failure = ApiResponseFactory.Failure<T>(ApiResponseCode.RESOURCE_CONFLICT, traceId);
        return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_CONFLICT].HttpStatus, failure);
    }
}
