using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Commands;

public record PostObjectiveElementEvidenceVersionMultipartCommand(
    Guid JourneyId,
    Guid ObjectiveElementId,
    Guid EvidenceId,
    string ApiVersion) : IRequest<IActionResult>;
