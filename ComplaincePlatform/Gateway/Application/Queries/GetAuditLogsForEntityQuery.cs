using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

public record GetAuditLogsForEntityQuery(
    string ApiVersion,
    string EntityType,
    Guid EntityId) : IRequest<IActionResult>;
