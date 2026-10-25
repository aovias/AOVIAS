using Gateway.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Commands;

public record CreateComplianceAuditFindingCommand(
    Guid JourneyId,
    CreateComplianceAuditFindingRequest Body,
    string ApiVersion) : IRequest<IActionResult>;
