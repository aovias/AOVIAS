using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Commands;

public record PatchJourneyOperationalModeCommand(Guid JourneyId, object Body, string ApiVersion) : IRequest<IActionResult>;

public record DeleteObjectiveElementEvidenceCommand(Guid JourneyId, Guid ObjectiveElementId, Guid EvidenceId, string ApiVersion) : IRequest<IActionResult>;
