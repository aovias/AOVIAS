using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Commands;

public record PostObjectiveElementEvidenceVerifyCommand(
    Guid JourneyId,
    Guid ObjectiveElementId,
    Guid EvidenceId,
    object Body,
    string ApiVersion) : IRequest<IActionResult>;
