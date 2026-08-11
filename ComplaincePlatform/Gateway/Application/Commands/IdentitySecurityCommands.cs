using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Commands;

public sealed record UpdateSecurityPolicyCommand(string ApiVersion, object RequestBody) : IRequest<IActionResult>;
public sealed record RevokeTrustedDeviceCommand(string ApiVersion, object RequestBody, Guid? UserId) : IRequest<IActionResult>;
public sealed record RevokeSecuritySessionCommand(string ApiVersion, object RequestBody, Guid? UserId) : IRequest<IActionResult>;
