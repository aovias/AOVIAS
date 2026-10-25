using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkforceService.Application.Commands;
using WorkforceService.Application.Contracts;
using WorkforceService.Application.DTOs;
using WorkforceService.Application.Queries;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.DTO;

namespace WorkforceService.Controllers.Internal;

[ApiController]
[Route("internal/employment")]
[Authorize(Policy = InternalApiAuthorizationExtensions.InternalOnlyPolicy)]
public class InternalEmploymentController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IEmploymentAppService _employmentAppService;

    public InternalEmploymentController(IMediator mediator, IEmploymentAppService employmentAppService)
    {
        _mediator = mediator;
        _employmentAppService = employmentAppService;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<EmploymentDto>>> Create(
        [FromBody] CreateEmploymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        try
        {
            var result = await _employmentAppService.CreateAsync(request, cancellationToken);
            var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, result, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, response);
        }
        catch (InvalidOperationException ex)
        {
            var failure = ApiResponseFactory.Failure<EmploymentDto>(ApiResponseCode.VALIDATION_FAILED, traceId);
            failure.Error!.Message = ex.Message;
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus, failure);
        }
    }

    [HttpPost("{id:guid}/assign-department")]
    public async Task<ActionResult<ApiResponse<EmploymentDto>>> AssignDepartment(
        Guid id,
        [FromBody] AssignDepartmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        try
        {
            var result = await _employmentAppService.AssignDepartmentAsync(id, request, cancellationToken);
            var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, result, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, response);
        }
        catch (InvalidOperationException ex)
        {
            var failure = ApiResponseFactory.Failure<EmploymentDto>(ApiResponseCode.VALIDATION_FAILED, traceId);
            failure.Error!.Message = ex.Message;
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus, failure);
        }
    }

    [HttpPost("{id:guid}/assign-position")]
    public async Task<ActionResult<ApiResponse<EmploymentDto>>> AssignPosition(
        Guid id,
        [FromBody] AssignPositionRequest request,
        CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        try
        {
            var result = await _employmentAppService.AssignPositionAsync(id, request, cancellationToken);
            var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, result, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, response);
        }
        catch (InvalidOperationException ex)
        {
            var failure = ApiResponseFactory.Failure<EmploymentDto>(ApiResponseCode.VALIDATION_FAILED, traceId);
            failure.Error!.Message = ex.Message;
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus, failure);
        }
    }

    [HttpPost("{id:guid}/assign-manager")]
    public async Task<ActionResult<ApiResponse<EmploymentDto>>> AssignManager(
        Guid id,
        [FromBody] AssignManagerRequest request,
        CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        try
        {
            var result = await _employmentAppService.AssignManagerAsync(id, request, cancellationToken);
            var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, result, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, response);
        }
        catch (InvalidOperationException ex)
        {
            var failure = ApiResponseFactory.Failure<EmploymentDto>(ApiResponseCode.VALIDATION_FAILED, traceId);
            failure.Error!.Message = ex.Message;
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus, failure);
        }
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

    [HttpGet("by-person/{personId:guid}")]
    public async Task<ActionResult<ApiResponse<PagedResultDto<EmploymentListItemDto>>>> ByPerson(Guid personId, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var all = await _mediator.Send(new SearchEmploymentsQuery(personId.ToString(), 1, 100), cancellationToken);
        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, all, traceId);
        return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, response);
    }

    [HttpPut("upsert")]
    public async Task<ActionResult<ApiResponse<IntegrationUpsertResponse>>> Upsert(
        [FromBody] IntegrationUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        try
        {
            var result = await _employmentAppService.UpsertFromIntegrationAsync(request, cancellationToken);
            var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, result, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, response);
        }
        catch (InvalidOperationException ex)
        {
            var failure = ApiResponseFactory.Failure<IntegrationUpsertResponse>(ApiResponseCode.VALIDATION_FAILED, traceId);
            failure.Error!.Message = ex.Message;
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus, failure);
        }
    }

    [HttpPost("{id:guid}/integration-rollback")]
    public async Task<ActionResult<ApiResponse<bool>>> Rollback(Guid id, [FromBody] IntegrationRollbackRequest request, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var ok = await _employmentAppService.RollbackIntegrationAsync(request.OrganizationId, id, cancellationToken);
        if (!ok)
        {
            var failure = ApiResponseFactory.Failure<bool>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND].HttpStatus, failure);
        }
        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, true, traceId);
        return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, response);
    }
}
