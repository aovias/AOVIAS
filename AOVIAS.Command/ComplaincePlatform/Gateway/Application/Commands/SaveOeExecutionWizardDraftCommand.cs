using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Commands;

public sealed record SaveOeExecutionWizardDraftCommand(
    Guid JourneyId,
    Guid ObjectiveElementId,
    object Body,
    string ApiVersion) : IRequest<IActionResult>;
