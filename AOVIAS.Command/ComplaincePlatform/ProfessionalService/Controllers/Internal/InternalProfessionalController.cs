using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProfessionalService.Application.DTOs;
using ProfessionalService.Application.Queries;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.DTO;

namespace ProfessionalService.Controllers.Internal;

[ApiController]
[Route("internal/professional")]
[Authorize(Policy = InternalApiAuthorizationExtensions.InternalOnlyPolicy)]
public class InternalProfessionalController : ControllerBase
{
    private readonly IMediator _mediator;

    public InternalProfessionalController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProfessionalProfileDto>>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var data = await _mediator.Send(new GetProfessionalProfileByIdQuery(id), cancellationToken);
        if (data == null) return NotFound(ApiResponseFactory.Failure<ProfessionalProfileDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId));
    }

    [HttpGet("by-employment/{employmentId:guid}")]
    public async Task<ActionResult<ApiResponse<ProfessionalProfileDto>>> ByEmployment(Guid employmentId, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var data = await _mediator.Send(new GetProfessionalProfileByEmploymentQuery(employmentId), cancellationToken);
        if (data == null) return NotFound(ApiResponseFactory.Failure<ProfessionalProfileDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId));
    }

    [HttpGet("by-person/{personId:guid}")]
    public async Task<ActionResult<ApiResponse<ProfessionalProfileDto>>> ByPerson(Guid personId, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var data = await _mediator.Send(new GetProfessionalProfileByPersonQuery(personId), cancellationToken);
        if (data == null) return NotFound(ApiResponseFactory.Failure<ProfessionalProfileDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId));
    }

    [HttpGet("expiring")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RenewalDashboardItemDto>>>> Expiring(
        [FromQuery] int withinDays = 90, [FromQuery] Guid? organizationId = null, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var data = await _mediator.Send(new GetRenewalDashboardQuery(withinDays, organizationId), cancellationToken);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId));
    }
}
