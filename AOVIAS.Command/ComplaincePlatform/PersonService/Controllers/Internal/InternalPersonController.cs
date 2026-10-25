using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonService.Application.Commands;
using PersonService.Application.Contracts;
using PersonService.Application.DTOs;
using PersonService.Application.Queries;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.DTO;

namespace PersonService.Controllers.Internal;

[ApiController]
[Route("internal/persons")]
[Authorize(Policy = InternalApiAuthorizationExtensions.InternalOnlyPolicy)]
public class InternalPersonController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IPersonAppService _personAppService;

    public InternalPersonController(IMediator mediator, IPersonAppService personAppService)
    {
        _mediator = mediator;
        _personAppService = personAppService;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<InternalCreatePersonResponse>>> Create(
        [FromBody] CreatePersonRequest request,
        CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        try
        {
            var result = await _mediator.Send(new CreatePersonCommand(request), cancellationToken);
            var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, result, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, response);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("email", StringComparison.OrdinalIgnoreCase))
        {
            var failure = ApiResponseFactory.Failure<InternalCreatePersonResponse>(ApiResponseCode.RESOURCE_CONFLICT, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_CONFLICT].HttpStatus, failure);
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PersonDto>>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var person = await _mediator.Send(new GetPersonByIdQuery(id), cancellationToken);
        if (person == null)
        {
            var failure = ApiResponseFactory.Failure<PersonDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND].HttpStatus, failure);
        }

        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, person, traceId);
        return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, response);
    }

    [HttpPut("upsert")]
    public async Task<ActionResult<ApiResponse<IntegrationUpsertResponse>>> Upsert(
        [FromBody] IntegrationUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var result = await _personAppService.UpsertFromIntegrationAsync(request, cancellationToken);
        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, result, traceId);
        return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, response);
    }

    [HttpPost("{id:guid}/integration-rollback")]
    public async Task<ActionResult<ApiResponse<bool>>> Rollback(Guid id, [FromBody] IntegrationRollbackRequest request, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var ok = await _personAppService.RollbackIntegrationAsync(request.OrganizationId, id, cancellationToken);
        if (!ok)
        {
            var failure = ApiResponseFactory.Failure<bool>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND].HttpStatus, failure);
        }
        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, true, traceId);
        return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, response);
    }
}
