using Gateway.Application.Commands;
using Gateway.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/employment")]
[Authorize]
public class EmploymentController : ControllerBase
{
    private readonly IMediator _mediator;
    public EmploymentController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] Guid? organizationId = null, [FromQuery] Guid? hospitalId = null, [FromQuery] Guid? statusValueId = null, CancellationToken cancellationToken = default)
        => _mediator.Send(new ListEmploymentsQuery(ApiVersion(), page, pageSize, organizationId, hospitalId, statusValueId), cancellationToken);

    [HttpGet("search")]
    public Task<IActionResult> Search([FromQuery(Name = "q")] string query, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        => _mediator.Send(new SearchEmploymentsQuery(ApiVersion(), query, page, pageSize), cancellationToken);

    [HttpGet("org-chart")]
    public Task<IActionResult> OrgChart([FromQuery] Guid organizationId, [FromQuery] Guid? hospitalId = null, [FromQuery] Guid? rootEmploymentId = null, CancellationToken cancellationToken = default)
        => _mediator.Send(new GetOrgChartQuery(ApiVersion(), organizationId, hospitalId, rootEmploymentId), cancellationToken);

    [HttpGet("manager/{managerEmploymentId:guid}/team")]
    public Task<IActionResult> ManagerTeam(Guid managerEmploymentId, CancellationToken cancellationToken = default)
        => _mediator.Send(new GetManagerTeamQuery(ApiVersion(), managerEmploymentId), cancellationToken);

    [HttpGet("positions")]
    public Task<IActionResult> Positions([FromQuery] Guid organizationId, CancellationToken cancellationToken = default)
        => _mediator.Send(new GetPositionsQuery(ApiVersion(), organizationId), cancellationToken);

    [HttpGet("lookups/{category}")]
    public Task<IActionResult> Lookups(string category, CancellationToken cancellationToken = default)
        => _mediator.Send(new GetWorkforceLookupsQuery(ApiVersion(), category), cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
        => _mediator.Send(new GetEmploymentByIdQuery(ApiVersion(), id), cancellationToken);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] object requestBody, CancellationToken cancellationToken = default)
        => _mediator.Send(new CreateEmploymentCommand(ApiVersion(), requestBody), cancellationToken);

    [HttpPut("{id:guid}")]
    public Task<IActionResult> Update(Guid id, [FromBody] object requestBody, CancellationToken cancellationToken = default)
        => _mediator.Send(new UpdateEmploymentCommand(ApiVersion(), id, requestBody), cancellationToken);

    [HttpPatch("{id:guid}")]
    public Task<IActionResult> Patch(Guid id, [FromBody] object requestBody, CancellationToken cancellationToken = default)
        => Update(id, requestBody, cancellationToken);

    [HttpDelete("{id:guid}")]
    public Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
        => _mediator.Send(new DeleteEmploymentCommand(ApiVersion(), id), cancellationToken);

    [HttpPost("{id:guid}/assign-position")]
    public Task<IActionResult> AssignPosition(Guid id, [FromBody] object requestBody, CancellationToken cancellationToken = default)
        => _mediator.Send(new AssignPositionCommand(ApiVersion(), id, requestBody), cancellationToken);

    [HttpPost("{id:guid}/assign-department")]
    public Task<IActionResult> AssignDepartment(Guid id, [FromBody] object requestBody, CancellationToken cancellationToken = default)
        => _mediator.Send(new AssignDepartmentCommand(ApiVersion(), id, requestBody), cancellationToken);

    [HttpPost("{id:guid}/assign-manager")]
    public Task<IActionResult> AssignManager(Guid id, [FromBody] object requestBody, CancellationToken cancellationToken = default)
        => _mediator.Send(new AssignManagerCommand(ApiVersion(), id, requestBody), cancellationToken);

    [HttpPost("{id:guid}/transfer")]
    public Task<IActionResult> Transfer(Guid id, [FromBody] object requestBody, CancellationToken cancellationToken = default)
        => _mediator.Send(new TransferEmploymentCommand(ApiVersion(), id, requestBody), cancellationToken);

    [HttpPost("{id:guid}/promote")]
    public Task<IActionResult> Promote(Guid id, [FromBody] object requestBody, CancellationToken cancellationToken = default)
        => _mediator.Send(new PromoteEmploymentCommand(ApiVersion(), id, requestBody), cancellationToken);

    [HttpGet("{id:guid}/history")]
    public Task<IActionResult> History(Guid id, CancellationToken cancellationToken = default)
        => _mediator.Send(new GetEmploymentHistoryQuery(ApiVersion(), id), cancellationToken);

    [HttpGet("{id:guid}/timeline")]
    public Task<IActionResult> Timeline(Guid id, CancellationToken cancellationToken = default)
        => _mediator.Send(new GetEmploymentTimelineQuery(ApiVersion(), id), cancellationToken);

    private string ApiVersion() => HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
}
