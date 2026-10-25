using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Commands;

public sealed record CreateComplianceTaskCommand(string ApiVersion, object RequestBody) : IRequest<IActionResult>;

public sealed record PatchComplianceTaskCommand(string ApiVersion, Guid TaskId, object RequestBody) : IRequest<IActionResult>;

public sealed record CompleteComplianceTaskCommand(string ApiVersion, Guid FrameworkId, Guid TaskId) : IRequest<IActionResult>;
