using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

public record GetJourneySessionQuery(Guid JourneyId, string ApiVersion, string? QuerySuffix) : IRequest<IActionResult>;

public record GetJourneyDashboardQuery(Guid JourneyId, string ApiVersion, string? QuerySuffix) : IRequest<IActionResult>;

public record GetJourneyChaptersSplitQuery(Guid JourneyId, string ApiVersion, string? QuerySuffix) : IRequest<IActionResult>;

public record GetObjectiveElementDetailQuery(Guid JourneyId, Guid ObjectiveElementId, string ApiVersion, string? QuerySuffix) : IRequest<IActionResult>;
