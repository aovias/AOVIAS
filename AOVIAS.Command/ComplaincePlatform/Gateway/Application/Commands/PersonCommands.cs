using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Commands;

public record CreatePersonCommand(string ApiVersion, object RequestBody) : IRequest<IActionResult>;
public record UpdatePersonCommand(string ApiVersion, Guid PersonId, object RequestBody) : IRequest<IActionResult>;
public record DeletePersonCommand(string ApiVersion, Guid PersonId) : IRequest<IActionResult>;
public record AddPersonDocumentCommand(string ApiVersion, Guid PersonId, object RequestBody) : IRequest<IActionResult>;
public record RemovePersonDocumentCommand(string ApiVersion, Guid PersonId, Guid DocumentId) : IRequest<IActionResult>;
