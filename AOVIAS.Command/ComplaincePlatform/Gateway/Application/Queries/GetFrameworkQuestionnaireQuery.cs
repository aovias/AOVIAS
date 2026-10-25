using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

public record GetFrameworkQuestionnaireQuery(Guid FrameworkId, string ApiVersion) : IRequest<IActionResult>;
