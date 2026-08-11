using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

public sealed record GetOeExecutionWizardQuery(
    Guid JourneyId,
    Guid ObjectiveElementId,
    string ApiVersion) : IRequest<IActionResult>;
