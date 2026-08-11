using IdentityService.Application.Contracts.Security;
using IdentityService.Domain.Entities;
using MediatR;

namespace IdentityService.Application.Security.Commands;

public sealed record UpdateSecurityPolicyCommand(Guid ActorUserId, bool IsAdmin, UpdateSecurityPolicyRequest Request) : IRequest<UserSecurityPolicy>;
public sealed record RevokeTrustedDeviceCommand(Guid ActorUserId, bool IsAdmin, Guid? UserId, RevokeDeviceRequest Request) : IRequest;
public sealed record RevokeSecuritySessionCommand(Guid ActorUserId, bool IsAdmin, Guid? UserId, RevokeSecuritySessionRequest Request) : IRequest;
