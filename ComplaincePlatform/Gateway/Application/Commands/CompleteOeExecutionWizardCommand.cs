using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Commands;

public sealed record CompleteOeExecutionWizardCommand(
    Guid JourneyId,
    Guid ObjectiveElementId,
    object Body,
    string ApiVersion,
    string? QuerySuffix) : IRequest<IActionResult>;
