using Gateway.Application.Commands;
using Gateway.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/professional-profiles")]
[Authorize]
public class ProfessionalProfilesController : ControllerBase
{
    private readonly IMediator _mediator;
    public ProfessionalProfilesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] Guid? categoryValueId = null, [FromQuery] Guid? specialtyValueId = null, CancellationToken cancellationToken = default)
        => _mediator.Send(new ListProfessionalProfilesQuery(ApiVersion(), page, pageSize, categoryValueId, specialtyValueId), cancellationToken);

    [HttpGet("search")]
    public Task<IActionResult> Search([FromQuery(Name = "q")] string query, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        => _mediator.Send(new SearchProfessionalProfilesQuery(ApiVersion(), query, page, pageSize), cancellationToken);

    [HttpGet("renewals")]
    public Task<IActionResult> Renewals([FromQuery] int withinDays = 90, [FromQuery] Guid? organizationId = null, CancellationToken cancellationToken = default)
        => _mediator.Send(new GetProfessionalRenewalsQuery(ApiVersion(), withinDays, organizationId), cancellationToken);

    [HttpGet("lookups/{category}")]
    public Task<IActionResult> Lookups(string category, CancellationToken cancellationToken = default)
        => _mediator.Send(new GetProfessionalLookupsQuery(ApiVersion(), category), cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
        => _mediator.Send(new GetProfessionalProfileByIdQuery(ApiVersion(), id), cancellationToken);

    [HttpGet("{id:guid}/timeline")]
    public Task<IActionResult> Timeline(Guid id, CancellationToken cancellationToken = default)
        => _mediator.Send(new GetProfessionalTimelineQuery(ApiVersion(), id), cancellationToken);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] object requestBody, CancellationToken cancellationToken = default)
        => _mediator.Send(new CreateProfessionalProfileCommand(ApiVersion(), requestBody), cancellationToken);

    [HttpPut("{id:guid}")]
    public Task<IActionResult> Update(Guid id, [FromBody] object requestBody, CancellationToken cancellationToken = default)
        => _mediator.Send(new UpdateProfessionalProfileCommand(ApiVersion(), id, requestBody), cancellationToken);

    [HttpDelete("{id:guid}")]
    public Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
        => _mediator.Send(new DeleteProfessionalProfileCommand(ApiVersion(), id), cancellationToken);

    [HttpPost("{id:guid}/qualifications")]
    public Task<IActionResult> AddQualification(Guid id, [FromBody] object requestBody, CancellationToken cancellationToken = default)
        => _mediator.Send(new AddQualificationCommand(ApiVersion(), id, requestBody), cancellationToken);

    [HttpPost("{id:guid}/registrations")]
    public Task<IActionResult> AddRegistration(Guid id, [FromBody] object requestBody, CancellationToken cancellationToken = default)
        => _mediator.Send(new AddRegistrationCommand(ApiVersion(), id, requestBody), cancellationToken);

    [HttpPost("{id:guid}/licenses")]
    public Task<IActionResult> AddLicense(Guid id, [FromBody] object requestBody, CancellationToken cancellationToken = default)
        => _mediator.Send(new AddLicenseCommand(ApiVersion(), id, requestBody), cancellationToken);

    [HttpPost("{id:guid}/certifications")]
    public Task<IActionResult> AddCertification(Guid id, [FromBody] object requestBody, CancellationToken cancellationToken = default)
        => _mediator.Send(new AddCertificationCommand(ApiVersion(), id, requestBody), cancellationToken);

    [HttpPost("{id:guid}/competencies")]
    public Task<IActionResult> AddCompetency(Guid id, [FromBody] object requestBody, CancellationToken cancellationToken = default)
        => _mediator.Send(new AddCompetencyCommand(ApiVersion(), id, requestBody), cancellationToken);

    [HttpPost("{id:guid}/privileges")]
    public Task<IActionResult> GrantPrivilege(Guid id, [FromBody] object requestBody, CancellationToken cancellationToken = default)
        => _mediator.Send(new GrantClinicalPrivilegeCommand(ApiVersion(), id, requestBody), cancellationToken);

    [HttpPost("{id:guid}/credentialing")]
    public Task<IActionResult> SubmitCredentialing(Guid id, [FromBody] object requestBody, CancellationToken cancellationToken = default)
        => _mediator.Send(new SubmitCredentialingCommand(ApiVersion(), id, requestBody), cancellationToken);

    [HttpPost("{id:guid}/training")]
    public Task<IActionResult> AddTraining(Guid id, [FromBody] object requestBody, CancellationToken cancellationToken = default)
        => _mediator.Send(new AddTrainingRecordCommand(ApiVersion(), id, requestBody), cancellationToken);

    private string ApiVersion() => HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
}
