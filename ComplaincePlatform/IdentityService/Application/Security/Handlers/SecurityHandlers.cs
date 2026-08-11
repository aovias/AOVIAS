using IdentityService.Application.Contracts;
using IdentityService.Application.Contracts.Security;
using IdentityService.Application.Security.Commands;
using IdentityService.Application.Security.Queries;
using IdentityService.Domain.Entities;
using MediatR;

namespace IdentityService.Application.Security.Handlers;

public static class SecurityAuthorization
{
    public static Guid ResolveTargetUser(Guid actorUserId, bool isAdmin, Guid? requestedUserId)
        => requestedUserId.HasValue && isAdmin ? requestedUserId.Value : actorUserId;

    public static void EnsureAdmin(bool isAdmin)
    {
        if (!isAdmin)
            throw new UnauthorizedAccessException("Administrator access required.");
    }
}

public sealed class ListSecurityEventsHandler(ISecurityEventService events) : IRequestHandler<ListSecurityEventsQuery, IReadOnlyList<SecurityEventDto>>
{
    public Task<IReadOnlyList<SecurityEventDto>> Handle(ListSecurityEventsQuery request, CancellationToken ct)
    {
        var userId = SecurityAuthorization.ResolveTargetUser(request.ActorUserId, request.IsAdmin, request.UserId);
        if (request.UserId.HasValue && request.UserId != request.ActorUserId)
            SecurityAuthorization.EnsureAdmin(request.IsAdmin);
        return events.ListAsync(userId, request.Page, request.PageSize, ct);
    }
}

public sealed class GetSecurityDashboardHandler(ISecurityDashboardService dashboard) : IRequestHandler<GetSecurityDashboardQuery, SecurityDashboardDto>
{
    public Task<SecurityDashboardDto> Handle(GetSecurityDashboardQuery request, CancellationToken ct)
    {
        SecurityAuthorization.EnsureAdmin(request.IsAdmin);
        return dashboard.GetSummaryAsync(ct);
    }
}

public sealed class GetUserRiskHandler(IRiskScoringService risk) : IRequestHandler<GetUserRiskQuery, UserRiskScoreDto?>
{
    public Task<UserRiskScoreDto?> Handle(GetUserRiskQuery request, CancellationToken ct)
    {
        if (request.UserId.HasValue && request.UserId != request.ActorUserId)
            SecurityAuthorization.EnsureAdmin(request.IsAdmin);
        var target = SecurityAuthorization.ResolveTargetUser(request.ActorUserId, request.IsAdmin, request.UserId);
        return risk.GetLatestAsync(target, ct);
    }
}

public sealed class ListTrustedDevicesHandler(ITrustedDeviceService devices) : IRequestHandler<ListTrustedDevicesQuery, IReadOnlyList<TrustedDeviceDto>>
{
    public Task<IReadOnlyList<TrustedDeviceDto>> Handle(ListTrustedDevicesQuery request, CancellationToken ct)
    {
        if (request.UserId.HasValue && request.UserId != request.ActorUserId)
            SecurityAuthorization.EnsureAdmin(request.IsAdmin);
        var target = SecurityAuthorization.ResolveTargetUser(request.ActorUserId, request.IsAdmin, request.UserId);
        return devices.ListAsync(target, ct);
    }
}

public sealed class GetSecurityTimelineHandler(ISecurityTimelineService timeline) : IRequestHandler<GetSecurityTimelineQuery, IReadOnlyList<SecurityTimelineDto>>
{
    public Task<IReadOnlyList<SecurityTimelineDto>> Handle(GetSecurityTimelineQuery request, CancellationToken ct)
    {
        if (request.UserId.HasValue && request.UserId != request.ActorUserId)
            SecurityAuthorization.EnsureAdmin(request.IsAdmin);
        var target = request.UserId.HasValue && request.IsAdmin ? request.UserId : request.ActorUserId;
        return timeline.GetTimelineAsync(target, request.Take, ct);
    }
}

public sealed class ListIdentityAuditLogsHandler(IIdentityAuditReadService audit) : IRequestHandler<ListIdentityAuditLogsQuery, IReadOnlyList<IdentityAuditLogDto>>
{
    public Task<IReadOnlyList<IdentityAuditLogDto>> Handle(ListIdentityAuditLogsQuery request, CancellationToken ct)
    {
        SecurityAuthorization.EnsureAdmin(request.IsAdmin);
        return audit.ListAsync(request.From, request.To, request.Take, ct);
    }
}

public sealed class UpdateSecurityPolicyHandler(ISecurityPolicyAdminService policies) : IRequestHandler<UpdateSecurityPolicyCommand, UserSecurityPolicy>
{
    public Task<UserSecurityPolicy> Handle(UpdateSecurityPolicyCommand request, CancellationToken ct)
    {
        SecurityAuthorization.EnsureAdmin(request.IsAdmin);
        return policies.UpdatePolicyAsync(request.Request, ct);
    }
}

public sealed class RevokeTrustedDeviceHandler(ITrustedDeviceService devices) : IRequestHandler<RevokeTrustedDeviceCommand>
{
    public Task Handle(RevokeTrustedDeviceCommand request, CancellationToken ct)
    {
        if (request.UserId.HasValue && request.UserId != request.ActorUserId)
            SecurityAuthorization.EnsureAdmin(request.IsAdmin);
        var target = SecurityAuthorization.ResolveTargetUser(request.ActorUserId, request.IsAdmin, request.UserId);
        return devices.RevokeAsync(target, request.Request.DeviceRecordId, ct);
    }
}

public sealed class RevokeSecuritySessionHandler(ISessionService sessions) : IRequestHandler<RevokeSecuritySessionCommand>
{
    public async Task Handle(RevokeSecuritySessionCommand request, CancellationToken ct)
    {
        if (request.UserId.HasValue && request.UserId != request.ActorUserId)
            SecurityAuthorization.EnsureAdmin(request.IsAdmin);

        var session = await sessions.GetSessionAsync(request.Request.SessionId, ct)
            ?? throw new InvalidOperationException("Session not found.");

        var target = SecurityAuthorization.ResolveTargetUser(request.ActorUserId, request.IsAdmin, request.UserId);
        if (session.UserId != target && !request.IsAdmin)
            throw new UnauthorizedAccessException("Cannot revoke another user's session.");

        await sessions.RevokeSessionAsync(request.Request.SessionId, ct);
    }
}
