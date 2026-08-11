using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

public record GetJourneyWorkspaceQuery(Guid JourneyId, string ApiVersion, string? QuerySuffix) : IRequest<IActionResult>;
