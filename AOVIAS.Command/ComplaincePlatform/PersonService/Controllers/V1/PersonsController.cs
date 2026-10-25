using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonService.Application.Commands;
using PersonService.Application.Contracts;
using PersonService.Application.DTOs;
using PersonService.Application.Queries;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.DTO;
using System.Security.Claims;

namespace PersonService.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/persons")]
[Authorize]
public class PersonsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IPersonAppService _personAppService;

    public PersonsController(IMediator mediator, IPersonAppService personAppService)
    {
        _mediator = mediator;
        _personAppService = personAppService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResultDto<PersonListItemDto>>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var data = await _mediator.Send(new ListPersonsQuery(page, pageSize, status), cancellationToken);
        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId);
        return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, response);
    }

    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<PagedResultDto<PersonListItemDto>>>> Search(
        [FromQuery(Name = "q")] string query,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var data = await _mediator.Send(new SearchPersonsQuery(query, page, pageSize), cancellationToken);
        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId);
        return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, response);
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

    [HttpPost]
    public async Task<ActionResult<ApiResponse<InternalCreatePersonResponse>>> Create(
        [FromBody] CreatePersonRequest request,
        CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        request.CreatedByUserId ??= GetActorUserId();
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

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PersonDto>>> Update(
        Guid id,
        [FromBody] UpdatePersonRequest request,
        CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        request.CreatedByUserId ??= GetActorUserId();
        try
        {
            var person = await _mediator.Send(new UpdatePersonCommand(id, request), cancellationToken);
            if (person == null)
            {
                var failure = ApiResponseFactory.Failure<PersonDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId);
                return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND].HttpStatus, failure);
            }

            var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, person, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, response);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("email", StringComparison.OrdinalIgnoreCase))
        {
            var failure = ApiResponseFactory.Failure<PersonDto>(ApiResponseCode.RESOURCE_CONFLICT, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_CONFLICT].HttpStatus, failure);
        }
    }

    [HttpPatch("{id:guid}")]
    public Task<ActionResult<ApiResponse<PersonDto>>> Patch(
        Guid id,
        [FromBody] UpdatePersonRequest request,
        CancellationToken cancellationToken = default)
        => Update(id, request, cancellationToken);

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var deleted = await _mediator.Send(new DeletePersonCommand(id, GetActorUserId()), cancellationToken);
        if (!deleted)
        {
            var failure = ApiResponseFactory.Failure<object>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND].HttpStatus, failure);
        }

        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, (object)new { id }, traceId);
        return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, response);
    }

    [HttpPost("{id:guid}/photo")]
    public async Task<ActionResult<ApiResponse<object>>> UploadPhoto(Guid id, IFormFile file, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (file == null || file.Length == 0)
        {
            var validation = ApiResponseFactory.Failure<object>(ApiResponseCode.VALIDATION_FAILED, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus, validation);
        }

        await using var stream = file.OpenReadStream();
        var ok = await _personAppService.UploadPhotoAsync(id, stream, file.FileName, file.ContentType, GetActorUserId(), cancellationToken);
        if (!ok)
        {
            var failure = ApiResponseFactory.Failure<object>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND].HttpStatus, failure);
        }

        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, (object)new { id }, traceId);
        return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, response);
    }

    [HttpPost("{id:guid}/documents")]
    public async Task<ActionResult<ApiResponse<PersonDocumentDto>>> AddDocument(
        Guid id,
        [FromBody] PersonDocumentDto document,
        CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        try
        {
            var result = await _mediator.Send(new AddPersonDocumentCommand(id, document), cancellationToken);
            if (result == null)
            {
                var failure = ApiResponseFactory.Failure<PersonDocumentDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId);
                return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND].HttpStatus, failure);
            }

            var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, result, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, response);
        }
        catch (InvalidOperationException)
        {
            var failure = ApiResponseFactory.Failure<PersonDocumentDto>(ApiResponseCode.RESOURCE_CONFLICT, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_CONFLICT].HttpStatus, failure);
        }
    }

    [HttpDelete("{id:guid}/documents/{docId:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> RemoveDocument(Guid id, Guid docId, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var removed = await _mediator.Send(new RemovePersonDocumentCommand(id, docId, GetActorUserId()), cancellationToken);
        if (!removed)
        {
            var failure = ApiResponseFactory.Failure<object>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND].HttpStatus, failure);
        }

        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, (object)new { id, docId }, traceId);
        return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, response);
    }

    [HttpGet("{id:guid}/contacts")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PersonContactDto>>>> GetContacts(Guid id, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var data = await _mediator.Send(new GetPersonContactsQuery(id), cancellationToken);
        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId);
        return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, response);
    }

    [HttpGet("{id:guid}/addresses")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PersonAddressDto>>>> GetAddresses(Guid id, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var data = await _mediator.Send(new GetPersonAddressesQuery(id), cancellationToken);
        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId);
        return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, response);
    }

    [HttpGet("{id:guid}/timeline")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PersonTimelineDto>>>> GetTimeline(Guid id, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var data = await _mediator.Send(new GetPersonTimelineQuery(id), cancellationToken);
        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId);
        return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, response);
    }

    [HttpGet("lookups/{category}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LookupValueDto>>>> GetLookups(string category, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var data = await _mediator.Send(new GetPersonLookupsQuery(category), cancellationToken);
        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId);
        return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, response);
    }

    private Guid? GetActorUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
