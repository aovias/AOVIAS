using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Commands;

public record PostObjectiveElementEvidenceCommand(
    Guid JourneyId,
    Guid ObjectiveElementId,
    object? Body,
    string ApiVersion) : IRequest<IActionResult>;
