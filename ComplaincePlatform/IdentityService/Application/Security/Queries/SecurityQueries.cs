using IdentityService.Application.Contracts.Security;
using MediatR;

namespace IdentityService.Application.Security.Queries;

public sealed record ListSecurityEventsQuery(Guid ActorUserId, bool IsAdmin, Guid? UserId, int Page, int PageSize) : IRequest<IReadOnlyList<SecurityEventDto>>;
public sealed record GetSecurityDashboardQuery(Guid ActorUserId, bool IsAdmin) : IRequest<SecurityDashboardDto>;
public sealed record GetUserRiskQuery(Guid ActorUserId, bool IsAdmin, Guid? UserId) : IRequest<UserRiskScoreDto?>;
public sealed record ListTrustedDevicesQuery(Guid ActorUserId, bool IsAdmin, Guid? UserId) : IRequest<IReadOnlyList<TrustedDeviceDto>>;
public sealed record GetSecurityTimelineQuery(Guid ActorUserId, bool IsAdmin, Guid? UserId, int Take) : IRequest<IReadOnlyList<SecurityTimelineDto>>;
public sealed record ListIdentityAuditLogsQuery(Guid ActorUserId, bool IsAdmin, DateTime? From, DateTime? To, int Take) : IRequest<IReadOnlyList<IdentityAuditLogDto>>;
