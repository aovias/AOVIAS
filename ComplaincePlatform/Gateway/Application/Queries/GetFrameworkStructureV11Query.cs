using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Domain.Enums;

namespace Gateway.Application.Queries;

public record GetFrameworkStructureV11Query(
    Guid FrameworkId,
    string ApiVersion,
    OperationalMode? OperationalMode,
    string? Classifications,
    int Page,
    int PageSize) : IRequest<IActionResult>;
