using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

public record GetObjectiveElementEvidenceDetailQuery(
    Guid JourneyId,
    Guid ObjectiveElementId,
    Guid EvidenceId,
    string ApiVersion) : IRequest<IActionResult>;
