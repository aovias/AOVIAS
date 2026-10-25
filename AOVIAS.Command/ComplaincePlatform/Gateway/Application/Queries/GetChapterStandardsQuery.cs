using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

public record GetChapterStandardsQuery(
    Guid JourneyId,
    Guid ChapterId,
    string ApiVersion,
    string? QuerySuffix) : IRequest<IActionResult>;
