using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

public record GetOnboardingQuestionnaireQuery(Guid FrameworkId, string ApiVersion) : IRequest<IActionResult>;
