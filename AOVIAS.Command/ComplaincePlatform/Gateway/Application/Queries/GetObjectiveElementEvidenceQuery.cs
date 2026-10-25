using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

public record GetObjectiveElementEvidenceQuery(
    Guid JourneyId,
    Guid ObjectiveElementId,
    string ApiVersion) : IRequest<IActionResult>;
