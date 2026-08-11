using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Commands;

public record CreateMembershipProxyCommand(string ApiVersion, object RequestBody) : IRequest<IActionResult>;
public record UpdateMembershipProxyCommand(string ApiVersion, Guid Id, object RequestBody) : IRequest<IActionResult>;
public record TransferMembershipProxyCommand(string ApiVersion, object RequestBody) : IRequest<IActionResult>;
public record CreateInvitationProxyCommand(string ApiVersion, object RequestBody) : IRequest<IActionResult>;
public record AcceptInvitationProxyCommand(string ApiVersion, Guid InvitationId) : IRequest<IActionResult>;
public record CreateOrganizationalUnitProxyCommand(string ApiVersion, object RequestBody) : IRequest<IActionResult>;
public record UpdateOrganizationalUnitProxyCommand(string ApiVersion, Guid Id, object RequestBody) : IRequest<IActionResult>;
public record MoveOrganizationalUnitProxyCommand(string ApiVersion, Guid Id, object RequestBody) : IRequest<IActionResult>;
public record SetOrganizationalUnitStatusProxyCommand(string ApiVersion, Guid Id, string Action) : IRequest<IActionResult>;
public record DeleteOrganizationalUnitProxyCommand(string ApiVersion, Guid Id) : IRequest<IActionResult>;
public record CreateOrgDepartmentProxyCommand(string ApiVersion, object RequestBody) : IRequest<IActionResult>;
