using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

public record GetCoreAssessmentQuery(Guid FrameworkId, int Count, string ApiVersion) : IRequest<IActionResult>;
