using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProfessionalService.Application.Commands;
using ProfessionalService.Application.DTOs;
using ProfessionalService.Application.Queries;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.DTO;
using System.Security.Claims;

namespace ProfessionalService.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/professional-profiles")]
[Authorize]
public class ProfessionalProfilesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProfessionalProfilesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResultDto<ProfessionalListItemDto>>>> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] Guid? categoryValueId = null, [FromQuery] Guid? specialtyValueId = null,
        CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var data = await _mediator.Send(new ListProfessionalProfilesQuery(page, pageSize, categoryValueId, specialtyValueId), cancellationToken);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId));
    }

    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<PagedResultDto<ProfessionalListItemDto>>>> Search(
        [FromQuery(Name = "q")] string query, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var data = await _mediator.Send(new SearchProfessionalProfilesQuery(query, page, pageSize), cancellationToken);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId));
    }

    [HttpGet("renewals")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RenewalDashboardItemDto>>>> Renewals(
        [FromQuery] int withinDays = 90, [FromQuery] Guid? organizationId = null, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var data = await _mediator.Send(new GetRenewalDashboardQuery(withinDays, organizationId), cancellationToken);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId));
    }

    [HttpGet("lookups/{category}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LookupValueDto>>>> Lookups(string category, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var data = await _mediator.Send(new GetProfessionalLookupsQuery(category), cancellationToken);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProfessionalProfileDto>>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var data = await _mediator.Send(new GetProfessionalProfileByIdQuery(id), cancellationToken);
        if (data == null) return NotFound(ApiResponseFactory.Failure<ProfessionalProfileDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId));
    }

    [HttpGet("{id:guid}/timeline")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProfessionalTimelineDto>>>> Timeline(Guid id, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var data = await _mediator.Send(new GetProfessionalTimelineQuery(id), cancellationToken);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProfessionalProfileDto>>> Create([FromBody] CreateProfessionalProfileRequest request, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        request.CreatedByUserId ??= ActorUserId();
        try
        {
            var data = await _mediator.Send(new CreateProfessionalProfileCommand(request), cancellationToken);
            var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, response);
        }
        catch (InvalidOperationException)
        {
            var failure = ApiResponseFactory.Failure<ProfessionalProfileDto>(ApiResponseCode.RESOURCE_CONFLICT, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_CONFLICT].HttpStatus, failure);
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProfessionalProfileDto>>> Update(Guid id, [FromBody] UpdateProfessionalProfileRequest request, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        request.UpdatedByUserId ??= ActorUserId();
        var data = await _mediator.Send(new UpdateProfessionalProfileCommand(id, request), cancellationToken);
        if (data == null) return NotFound(ApiResponseFactory.Failure<ProfessionalProfileDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var deleted = await _mediator.Send(new DeleteProfessionalProfileCommand(id, ActorUserId()), cancellationToken);
        if (!deleted) return NotFound(ApiResponseFactory.Failure<bool>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, true, traceId));
    }

    [HttpPost("{id:guid}/qualifications")]
    public async Task<ActionResult<ApiResponse<QualificationDto>>> AddQualification(Guid id, [FromBody] AddQualificationRequest request, CancellationToken cancellationToken = default)
    {
        request.ActorUserId ??= ActorUserId();
        return await SubResource(id, new AddQualificationCommand(id, request), cancellationToken);
    }

    [HttpPost("{id:guid}/registrations")]
    public async Task<ActionResult<ApiResponse<RegistrationDto>>> AddRegistration(Guid id, [FromBody] AddRegistrationRequest request, CancellationToken cancellationToken = default)
    {
        request.ActorUserId ??= ActorUserId();
        return await SubResource(id, new AddRegistrationCommand(id, request), cancellationToken);
    }

    [HttpPost("{id:guid}/licenses")]
    public async Task<ActionResult<ApiResponse<LicenseDto>>> AddLicense(Guid id, [FromBody] AddLicenseRequest request, CancellationToken cancellationToken = default)
    {
        request.ActorUserId ??= ActorUserId();
        return await SubResource(id, new AddLicenseCommand(id, request), cancellationToken);
    }

    [HttpPost("{id:guid}/certifications")]
    public async Task<ActionResult<ApiResponse<CertificationDto>>> AddCertification(Guid id, [FromBody] AddCertificationRequest request, CancellationToken cancellationToken = default)
    {
        request.ActorUserId ??= ActorUserId();
        return await SubResource(id, new AddCertificationCommand(id, request), cancellationToken);
    }

    [HttpPost("{id:guid}/competencies")]
    public async Task<ActionResult<ApiResponse<CompetencyDto>>> AddCompetency(Guid id, [FromBody] AddCompetencyRequest request, CancellationToken cancellationToken = default)
    {
        request.ActorUserId ??= ActorUserId();
        return await SubResource(id, new AddCompetencyCommand(id, request), cancellationToken);
    }

    [HttpPost("{id:guid}/privileges")]
    public async Task<ActionResult<ApiResponse<ClinicalPrivilegeDto>>> GrantPrivilege(Guid id, [FromBody] GrantClinicalPrivilegeRequest request, CancellationToken cancellationToken = default)
    {
        request.ActorUserId ??= ActorUserId();
        return await SubResource(id, new GrantClinicalPrivilegeCommand(id, request), cancellationToken);
    }

    [HttpPost("{id:guid}/credentialing")]
    public async Task<ActionResult<ApiResponse<CredentialingRecordDto>>> SubmitCredentialing(Guid id, [FromBody] SubmitCredentialingRequest request, CancellationToken cancellationToken = default)
    {
        request.ActorUserId ??= ActorUserId();
        return await SubResource(id, new SubmitCredentialingCommand(id, request), cancellationToken);
    }

    [HttpPost("{id:guid}/training")]
    public async Task<ActionResult<ApiResponse<TrainingRecordDto>>> AddTraining(Guid id, [FromBody] AddTrainingRecordRequest request, CancellationToken cancellationToken = default)
    {
        request.ActorUserId ??= ActorUserId();
        return await SubResource(id, new AddTrainingRecordCommand(id, request), cancellationToken);
    }

    private async Task<ActionResult<ApiResponse<T>>> SubResource<T>(Guid id, IRequest<T?> command, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        try
        {
            var data = await _mediator.Send(command, cancellationToken);
            if (data == null) return NotFound(ApiResponseFactory.Failure<T>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
            return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId));
        }
        catch (InvalidOperationException)
        {
            var failure = ApiResponseFactory.Failure<T>(ApiResponseCode.RESOURCE_CONFLICT, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_CONFLICT].HttpStatus, failure);
        }
    }

    private Guid? ActorUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
