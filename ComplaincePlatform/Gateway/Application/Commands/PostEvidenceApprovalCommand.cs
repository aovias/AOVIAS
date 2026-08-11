using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Commands;

public sealed record PostEvidenceApprovalCommand(
    string ApiVersion,
    Guid EvidenceId,
    object Body) : IRequest<IActionResult>;
