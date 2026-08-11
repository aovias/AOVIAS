using Shared.Logging;

using IdentityService.Application.Contracts;

using IdentityService.Application.Contracts.Security;

using IdentityService.Application.DTOs;

using IdentityService.Domain.Entities;

using IdentityService.Domain.Enums;

using IdentityService.Infrastructure;

using IdentityService.Infrastructure.Security;

using Microsoft.EntityFrameworkCore;



namespace IdentityService.Application.Services;



public class SessionService : ISessionService

{

    private readonly IdentityDbContext _db;

    private readonly ISecurityPolicyService _policyService;

    private readonly IIdentityAuditService _auditService;

    private readonly IThreatDetectionService _threatDetection;

    private readonly ISessionHistoryService _sessionHistory;



    public SessionService(

        IdentityDbContext db,

        ISecurityPolicyService policyService,

        IIdentityAuditService auditService,

        IThreatDetectionService threatDetection,

        ISessionHistoryService sessionHistory)

    {

        _db = db;

        _policyService = policyService;

        _auditService = auditService;

        _threatDetection = threatDetection;

        _sessionHistory = sessionHistory;

    }



    public async Task<(UserSession session, string refreshToken)> CreateSessionAsync(

        Guid userId,

        string? deviceId,

        string? ipAddress,

        string? userAgent,

        CancellationToken cancellationToken = default)

    {

        var policy = await _policyService.GetEffectivePolicyAsync(cancellationToken);

        await EnforceConcurrentSessionLimitAsync(userId, policy.MaxConcurrentSessions, cancellationToken);



        var refreshToken = TokenHasher.GenerateOpaqueToken();

        var now = DateTime.UtcNow;

        var geo = ClientContextParser.ResolveGeoLocation(ipAddress);

        var session = new UserSession

        {

            SessionId = Guid.NewGuid(),

            UserId = userId,

            RefreshTokenHash = TokenHasher.Hash(refreshToken),

            TokenFamilyId = Guid.NewGuid(),

            DeviceId = deviceId,

            IPAddress = ipAddress,

            UserAgent = userAgent,

            DeviceFingerprint = ClientContextParser.BuildFingerprint(deviceId, userAgent, ipAddress),

            GeoLocation = geo,

            Browser = ClientContextParser.ParseBrowser(userAgent),

            OperatingSystem = ClientContextParser.ParseOperatingSystem(userAgent),

            CreatedAt = now,

            ExpiresAt = now.AddDays(policy.RefreshTokenAbsoluteExpirationDays),

            LastActivityAt = now,

            Status = SessionStatus.Active

        };



        _db.UserSessions.Add(session);

        await _db.SaveChangesAsync(cancellationToken);

        await _sessionHistory.AppendAsync(session.SessionId, userId, SessionHistoryAction.Created, "Session created", ipAddress, geo, session.RiskScore, cancellationToken);

        return (session, refreshToken);

    }



    public async Task<(RefreshResultDto? rotation, Guid userId, Guid sessionId)?> RotateRefreshTokenAsync(

        string refreshToken,

        CancellationToken cancellationToken = default)

    {

        var hash = TokenHasher.Hash(refreshToken);

        var session = await _db.UserSessions.FirstOrDefaultAsync(s => s.RefreshTokenHash == hash, cancellationToken);

        if (session == null)

            return null;



        if (session.Status == SessionStatus.Revoked)

        {

            await RevokeTokenFamilyAsync(session.TokenFamilyId, cancellationToken);

            await _auditService.RecordFailureAsync(EventCode.AuthFailed, "RefreshToken", "ReuseDetected",

                targetUserId: session.UserId, sessionId: session.SessionId, cancellationToken: cancellationToken);

            await _threatDetection.EvaluateRefreshReuseAsync(session.UserId, session.SessionId, cancellationToken);

            return null;

        }



        if (session.Status != SessionStatus.Active || session.ExpiresAt <= DateTime.UtcNow)

        {

            if (session.Status == SessionStatus.Active)

            {

                session.Status = SessionStatus.Expired;

                await _db.SaveChangesAsync(cancellationToken);

            }

            return null;

        }



        var now = DateTime.UtcNow;

        var affected = await _db.UserSessions

            .Where(s => s.RefreshTokenHash == hash && s.Status == SessionStatus.Active && s.ExpiresAt > now)

            .ExecuteUpdateAsync(setters => setters

                .SetProperty(s => s.Status, SessionStatus.Revoked)

                .SetProperty(s => s.RevokedAt, now), cancellationToken);



        if (affected == 0)

        {

            var current = await _db.UserSessions.FirstOrDefaultAsync(s => s.RefreshTokenHash == hash, cancellationToken);

            if (current?.Status == SessionStatus.Revoked)

            {

                await RevokeTokenFamilyAsync(current.TokenFamilyId, cancellationToken);

                await _auditService.RecordFailureAsync(EventCode.AuthFailed, "RefreshToken", "ReuseDetected",

                    targetUserId: current.UserId, sessionId: current.SessionId, cancellationToken: cancellationToken);

                await _threatDetection.EvaluateRefreshReuseAsync(current.UserId, current.SessionId, cancellationToken);

            }

            return null;

        }



        var policy = await _policyService.GetEffectivePolicyAsync(cancellationToken);

        var newRefreshToken = TokenHasher.GenerateOpaqueToken();

        var slidingExpiry = now.AddMinutes(policy.RefreshTokenSlidingExpirationMinutes);

        var absoluteExpiry = session.CreatedAt.AddDays(policy.RefreshTokenAbsoluteExpirationDays);

        var newExpiresAt = slidingExpiry < absoluteExpiry ? slidingExpiry : absoluteExpiry;



        var newSession = new UserSession

        {

            SessionId = Guid.NewGuid(),

            UserId = session.UserId,

            RefreshTokenHash = TokenHasher.Hash(newRefreshToken),

            TokenFamilyId = session.TokenFamilyId,

            DeviceId = session.DeviceId,

            IPAddress = session.IPAddress,

            UserAgent = session.UserAgent,

            DeviceFingerprint = session.DeviceFingerprint,

            GeoLocation = session.GeoLocation,

            Browser = session.Browser,

            OperatingSystem = session.OperatingSystem,

            RiskScore = session.RiskScore,

            IsSuspicious = session.IsSuspicious,

            CreatedAt = session.CreatedAt,

            ExpiresAt = newExpiresAt,

            LastActivityAt = now,

            Status = SessionStatus.Active

        };



        _db.UserSessions.Add(newSession);

        await _db.SaveChangesAsync(cancellationToken);

        await _sessionHistory.AppendAsync(newSession.SessionId, session.UserId, SessionHistoryAction.Refreshed, "Token rotated", session.IPAddress, session.GeoLocation, session.RiskScore, cancellationToken);



        return (new RefreshResultDto

        {

            AccessToken = string.Empty,

            RefreshToken = newRefreshToken,

            ExpiresIn = 0,

            UserId = session.UserId,

            SessionId = newSession.SessionId

        }, session.UserId, newSession.SessionId);

    }



    public async Task RevokeSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)

    {

        var session = await _db.UserSessions.FirstOrDefaultAsync(s => s.SessionId == sessionId, cancellationToken);

        if (session == null || session.Status != SessionStatus.Active)

            return;



        session.Status = SessionStatus.Revoked;

        session.RevokedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        await _sessionHistory.AppendAsync(sessionId, session.UserId, SessionHistoryAction.Revoked, "Session revoked", session.IPAddress, session.GeoLocation, session.RiskScore, cancellationToken);

    }



    public async Task RevokeSessionByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)

    {

        var hash = TokenHasher.Hash(refreshToken);

        var session = await _db.UserSessions.FirstOrDefaultAsync(s => s.RefreshTokenHash == hash, cancellationToken);

        if (session == null)

            return;



        await RevokeSessionAsync(session.SessionId, cancellationToken);

    }



    public async Task RevokeAllSessionsAsync(Guid userId, CancellationToken cancellationToken = default)

    {

        var sessions = await _db.UserSessions

            .Where(s => s.UserId == userId && s.Status == SessionStatus.Active)

            .ToListAsync(cancellationToken);



        foreach (var session in sessions)

        {

            session.Status = SessionStatus.Revoked;

            session.RevokedAt = DateTime.UtcNow;

            await _sessionHistory.AppendAsync(session.SessionId, userId, SessionHistoryAction.Revoked, "Logout all", session.IPAddress, session.GeoLocation, session.RiskScore, cancellationToken);

        }



        await _db.SaveChangesAsync(cancellationToken);

    }



    public async Task<IReadOnlyList<SessionDto>> ListSessionsAsync(Guid userId, Guid? currentSessionId, CancellationToken cancellationToken = default)

    {

        return await _db.UserSessions

            .Where(s => s.UserId == userId && s.Status == SessionStatus.Active)

            .OrderByDescending(s => s.LastActivityAt)

            .Select(s => new SessionDto

            {

                SessionId = s.SessionId,

                DeviceId = s.DeviceId,

                IPAddress = s.IPAddress,

                UserAgent = s.UserAgent,

                CreatedAt = s.CreatedAt,

                ExpiresAt = s.ExpiresAt,

                LastActivityAt = s.LastActivityAt,

                IsCurrent = currentSessionId.HasValue && s.SessionId == currentSessionId.Value

            })

            .ToListAsync(cancellationToken);

    }



    public Task<UserSession?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)

        => _db.UserSessions.FirstOrDefaultAsync(s => s.SessionId == sessionId, cancellationToken);



    private async Task EnforceConcurrentSessionLimitAsync(Guid userId, int maxSessions, CancellationToken cancellationToken)

    {

        var active = await _db.UserSessions

            .Where(s => s.UserId == userId && s.Status == SessionStatus.Active)

            .OrderBy(s => s.LastActivityAt)

            .ToListAsync(cancellationToken);



        while (active.Count >= maxSessions && active.Count > 0)

        {

            var oldest = active[0];

            oldest.Status = SessionStatus.Revoked;

            oldest.RevokedAt = DateTime.UtcNow;

            active.RemoveAt(0);

        }

    }



    private async Task RevokeTokenFamilyAsync(Guid familyId, CancellationToken cancellationToken)

    {

        var sessions = await _db.UserSessions

            .Where(s => s.TokenFamilyId == familyId && s.Status == SessionStatus.Active)

            .ToListAsync(cancellationToken);



        foreach (var session in sessions)

        {

            session.Status = SessionStatus.ReuseDetected;

            session.RevokedAt = DateTime.UtcNow;

        }



        await _db.SaveChangesAsync(cancellationToken);

    }

}


