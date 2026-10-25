using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Commands;

public sealed record PostEvidenceBinderOeEvidenceMultipartCommand(
    Guid FrameworkId,
    Guid ObjectiveElementId,
    string ApiVersion) : IRequest<IActionResult>;
