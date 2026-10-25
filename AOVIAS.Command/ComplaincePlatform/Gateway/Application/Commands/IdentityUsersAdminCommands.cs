using Gateway.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Commands;

public record CreateIdentityUserAdminCommand(string ApiVersion, Guid OrganizationId, Guid ActorUserId, CreateIdentityUserAdminRequest Request) : IRequest<IActionResult>;
public record UpdateIdentityUserAdminCommand(string ApiVersion, Guid OrganizationId, Guid UserId, Guid ActorUserId, UpdateIdentityUserAdminRequest Request) : IRequest<IActionResult>;
public record SuspendIdentityUserAdminCommand(string ApiVersion, Guid UserId, Guid ActorUserId) : IRequest<IActionResult>;
public record ActivateIdentityUserAdminCommand(string ApiVersion, Guid UserId, Guid ActorUserId) : IRequest<IActionResult>;
public record DisableIdentityUserAdminCommand(string ApiVersion, Guid UserId, Guid ActorUserId) : IRequest<IActionResult>;
public record DeleteIdentityUserAdminCommand(string ApiVersion, Guid OrganizationId, Guid UserId, Guid ActorUserId) : IRequest<IActionResult>;
public record UnlockIdentityUserAdminCommand(string ApiVersion, Guid UserId, Guid ActorUserId) : IRequest<IActionResult>;
public record AdminResetPasswordCommand(string ApiVersion, Guid UserId, Guid ActorUserId) : IRequest<IActionResult>;
public record ForcePasswordChangeIdentityCommand(string ApiVersion, Guid UserId, Guid ActorUserId) : IRequest<IActionResult>;
public record PlaceholderPasswordManagementCommand(string ApiVersion, string Feature) : IRequest<IActionResult>;
