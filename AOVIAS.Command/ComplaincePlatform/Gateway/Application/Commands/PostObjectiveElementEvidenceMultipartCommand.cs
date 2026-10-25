using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Commands;

public record PostObjectiveElementEvidenceMultipartCommand(Guid JourneyId, Guid ObjectiveElementId, string ApiVersion) : IRequest<IActionResult>;
