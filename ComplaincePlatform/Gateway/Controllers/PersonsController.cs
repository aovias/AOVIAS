using Gateway.Application.Commands;
using Gateway.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/persons")]
[Authorize]
public class PersonsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PersonsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null, CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(new ListPersonsQuery(apiVersion, page, pageSize, status), cancellationToken);
    }

    [HttpGet("search")]
    public Task<IActionResult> Search([FromQuery(Name = "q")] string query, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(new SearchPersonsQuery(apiVersion, query, page, pageSize), cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(new GetPersonByIdQuery(apiVersion, id), cancellationToken);
    }

    [HttpPost]
    public Task<IActionResult> Create([FromBody] object requestBody, CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(new CreatePersonCommand(apiVersion, requestBody), cancellationToken);
    }

    [HttpPut("{id:guid}")]
    public Task<IActionResult> Update(Guid id, [FromBody] object requestBody, CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(new UpdatePersonCommand(apiVersion, id, requestBody), cancellationToken);
    }

    [HttpPatch("{id:guid}")]
    public Task<IActionResult> Patch(Guid id, [FromBody] object requestBody, CancellationToken cancellationToken = default)
        => Update(id, requestBody, cancellationToken);

    [HttpDelete("{id:guid}")]
    public Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(new DeletePersonCommand(apiVersion, id), cancellationToken);
    }

    [HttpGet("{id:guid}/contacts")]
    public Task<IActionResult> Contacts(Guid id, CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(new GetPersonContactsQuery(apiVersion, id), cancellationToken);
    }

    [HttpGet("{id:guid}/addresses")]
    public Task<IActionResult> Addresses(Guid id, CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(new GetPersonAddressesQuery(apiVersion, id), cancellationToken);
    }

    [HttpGet("{id:guid}/timeline")]
    public Task<IActionResult> Timeline(Guid id, CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(new GetPersonTimelineQuery(apiVersion, id), cancellationToken);
    }

    [HttpGet("lookups/{category}")]
    public Task<IActionResult> Lookups(string category, CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(new GetPersonLookupsQuery(apiVersion, category), cancellationToken);
    }

    [HttpPost("{id:guid}/documents")]
    public Task<IActionResult> AddDocument(Guid id, [FromBody] object requestBody, CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(new AddPersonDocumentCommand(apiVersion, id, requestBody), cancellationToken);
    }

    [HttpDelete("{id:guid}/documents/{docId:guid}")]
    public Task<IActionResult> RemoveDocument(Guid id, Guid docId, CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(new RemovePersonDocumentCommand(apiVersion, id, docId), cancellationToken);
    }
}
