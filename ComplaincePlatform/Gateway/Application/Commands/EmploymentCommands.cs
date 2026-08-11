using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Commands;

public record CreateEmploymentCommand(string ApiVersion, object RequestBody) : IRequest<IActionResult>;
public record UpdateEmploymentCommand(string ApiVersion, Guid EmploymentId, object RequestBody) : IRequest<IActionResult>;
public record DeleteEmploymentCommand(string ApiVersion, Guid EmploymentId) : IRequest<IActionResult>;
public record AssignPositionCommand(string ApiVersion, Guid EmploymentId, object RequestBody) : IRequest<IActionResult>;
public record AssignDepartmentCommand(string ApiVersion, Guid EmploymentId, object RequestBody) : IRequest<IActionResult>;
public record AssignManagerCommand(string ApiVersion, Guid EmploymentId, object RequestBody) : IRequest<IActionResult>;
public record TransferEmploymentCommand(string ApiVersion, Guid EmploymentId, object RequestBody) : IRequest<IActionResult>;
public record PromoteEmploymentCommand(string ApiVersion, Guid EmploymentId, object RequestBody) : IRequest<IActionResult>;
