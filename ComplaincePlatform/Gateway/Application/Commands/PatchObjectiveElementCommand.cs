using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Commands;

public record PatchObjectiveElementCommand(
    Guid JourneyId,
    Guid ObjectiveElementId,
    object? Body,
    string ApiVersion,
    string? QuerySuffix) : IRequest<IActionResult>;
