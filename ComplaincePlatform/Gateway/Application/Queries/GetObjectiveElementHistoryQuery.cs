using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

public record GetObjectiveElementHistoryQuery(
    Guid JourneyId,
    Guid ObjectiveElementId,
    string ApiVersion) : IRequest<IActionResult>;
