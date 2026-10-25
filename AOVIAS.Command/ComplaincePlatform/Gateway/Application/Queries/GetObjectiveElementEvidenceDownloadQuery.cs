using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

public record GetObjectiveElementEvidenceDownloadQuery(
    Guid JourneyId,
    Guid ObjectiveElementId,
    Guid EvidenceId,
    string ApiVersion) : IRequest<IActionResult>;
