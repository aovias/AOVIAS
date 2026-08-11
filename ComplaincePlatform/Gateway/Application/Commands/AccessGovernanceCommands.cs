using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Commands;

public record CreateRoleCommand(string ApiVersion, object RequestBody) : IRequest<IActionResult>;
public record AssignRoleCommand(string ApiVersion, Guid UserId, Guid RoleId, object RequestBody) : IRequest<IActionResult>;
public record GrantPermissionToRoleCommand(string ApiVersion, Guid RoleId, Guid PermissionId) : IRequest<IActionResult>;
public record CreatePermissionCommand(string ApiVersion, object RequestBody) : IRequest<IActionResult>;
public record CreatePolicyCommand(string ApiVersion, object RequestBody) : IRequest<IActionResult>;
public record PublishPolicyCommand(string ApiVersion, Guid PolicyId) : IRequest<IActionResult>;
public record UpdatePolicyCommand(string ApiVersion, Guid PolicyId, object RequestBody) : IRequest<IActionResult>;
public record ArchivePolicyCommand(string ApiVersion, Guid PolicyId) : IRequest<IActionResult>;
public record AssignRoleAssignmentCommand(string ApiVersion, object RequestBody) : IRequest<IActionResult>;
public record RevokeRoleAssignmentCommand(string ApiVersion, Guid AssignmentId) : IRequest<IActionResult>;
public record CreateDelegationCommand(string ApiVersion, object RequestBody) : IRequest<IActionResult>;
public record RevokeDelegationCommand(string ApiVersion, Guid DelegationId) : IRequest<IActionResult>;
public record AssignResponsibilityCommand(string ApiVersion, object RequestBody) : IRequest<IActionResult>;
public record SetApprovalAuthorityCommand(string ApiVersion, object RequestBody) : IRequest<IActionResult>;
public record EvaluateAuthorizationCommand(string ApiVersion, object RequestBody) : IRequest<IActionResult>;
