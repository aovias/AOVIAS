using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

public record GetAuditLogsQuery(
    string ApiVersion,
    string? EntityType,
    Guid? EntityId,
    DateTime? From,
    DateTime? To) : IRequest<IActionResult>;
