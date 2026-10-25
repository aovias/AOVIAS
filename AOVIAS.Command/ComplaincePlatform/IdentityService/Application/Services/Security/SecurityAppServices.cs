using IdentityService.Application.Contracts;
using IdentityService.Application.Contracts.Security;
using IdentityService.Domain;
using IdentityService.Domain.Entities;
using IdentityService.Domain.Enums;
using IdentityService.Infrastructure;
using IdentityService.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace IdentityService.Application.Services.Security;

public class SecurityEventService(IdentityDbContext db) : ISecurityEventService
{
    public async Task RecordAsync(string eventType, SecurityEventSeverity severity, string summary, Guid? userId = null, Guid? sessionId = null, string? deviceId = null, string? ipAddress = null, object? payload = null, CancellationToken ct = default)
    {
        var evt = new SecurityEvent
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            Severity = severity,
            UserId = userId,
            SessionId = sessionId,
            DeviceId = deviceId,
            Summary = summary,
            IPAddress = ipAddress,
            PayloadJson = payload == null ? "{}" : JsonSerializer.Serialize(payload),
            OccurredAt = DateTime.UtcNow
        };
        db.SecurityEvents.Add(evt);
        await WriteOutboxAsync("SecurityEventRaised", evt.Id, new { eventType, userId }, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<SecurityEventDto>> ListAsync(Guid? userId, int page, int pageSize, CancellationToken ct = default)
    {
        var q = db.SecurityEvents.AsNoTracking();
        if (userId.HasValue) q = q.Where(e => e.UserId == userId);
        return await q.OrderByDescending(e => e.OccurredAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(e => new SecurityEventDto
            {
                Id = e.Id, EventType = e.EventType, Severity = e.Severity.ToString(),
                UserId = e.UserId, Summary = e.Summary, OccurredAt = e.OccurredAt, PayloadJson = e.PayloadJson
            }).ToListAsync(ct);
    }

    public Task WriteOutboxAsync(string type, Guid aggregateId, object payload, CancellationToken ct = default)
    {
        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateId = aggregateId,
            OccurredOn = DateTime.UtcNow,
            Type = type,
            Payload = JsonSerializer.Serialize(payload)
        });
        return Task.CompletedTask;
    }
}

public class SessionHistoryService(IdentityDbContext db) : ISessionHistoryService
{
    public async Task AppendAsync(Guid sessionId, Guid userId, SessionHistoryAction action, string? detail = null, string? ip = null, string? geo = null, int? riskScore = null, CancellationToken ct = default)
    {
        db.SessionHistories.Add(new SessionHistory
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            UserId = userId,
            Action = action,
            Detail = detail,
            IPAddress = ip,
            GeoLocation = geo,
            RiskScore = riskScore,
            OccurredAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }
}

public class TrustedDeviceService(IdentityDbContext db, ISecurityPolicyService policyService, ISecurityEventService events) : ITrustedDeviceService
{
    public async Task RegisterOrUpdateAsync(Guid userId, string deviceId, string? userAgent, string? ipAddress, bool trustDevice, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(deviceId)) return;
        var policy = await policyService.GetEffectivePolicyAsync(ct);
        var now = DateTime.UtcNow;
        var ua = userAgent;
        var existing = await db.TrustedDevices.FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceId == deviceId && !d.IsDeleted, ct);
        if (existing != null)
        {
            existing.LastSeenAt = now;
            existing.UserAgent = ua;
            existing.Browser = ClientContextParser.ParseBrowser(ua);
            existing.OperatingSystem = ClientContextParser.ParseOperatingSystem(ua);
            existing.Platform = ClientContextParser.ParsePlatform(ua);
            existing.Fingerprint = ClientContextParser.BuildFingerprint(deviceId, ua, ipAddress);
            if (trustDevice) existing.TrustedUntil = now.AddDays(policy.DeviceTrustDurationDays);
        }
        else if (trustDevice)
        {
            var device = new TrustedDevice
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DeviceId = deviceId,
                UserAgent = ua,
                Browser = ClientContextParser.ParseBrowser(ua),
                OperatingSystem = ClientContextParser.ParseOperatingSystem(ua),
                Platform = ClientContextParser.ParsePlatform(ua),
                Fingerprint = ClientContextParser.BuildFingerprint(deviceId, ua, ipAddress),
                VerificationStatus = DeviceVerificationStatus.Verified,
                TrustedUntil = now.AddDays(policy.DeviceTrustDurationDays),
                LastSeenAt = now,
                CreatedAt = now
            };
            db.TrustedDevices.Add(device);
            await events.RecordAsync("DeviceRegistered", SecurityEventSeverity.Info, "Device trusted", userId, deviceId: deviceId, ipAddress: ipAddress, ct: ct);
            await events.WriteOutboxAsync("DeviceTrusted", device.Id, new { userId, deviceId }, ct);
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> IsTrustedAsync(Guid userId, string deviceId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await db.TrustedDevices.AnyAsync(d =>
            d.UserId == userId && d.DeviceId == deviceId && !d.IsDeleted &&
            d.VerificationStatus == DeviceVerificationStatus.Verified &&
            d.TrustedUntil > now && d.RevokedAt == null, ct);
    }

    public async Task<IReadOnlyList<TrustedDeviceDto>> ListAsync(Guid userId, CancellationToken ct = default)
        => await db.TrustedDevices.AsNoTracking()
            .Where(d => d.UserId == userId && !d.IsDeleted && d.RevokedAt == null)
            .OrderByDescending(d => d.LastSeenAt)
            .Select(d => new TrustedDeviceDto
            {
                Id = d.Id, DeviceId = d.DeviceId, Nickname = d.Nickname, Platform = d.Platform,
                Browser = d.Browser, RiskLevel = d.RiskLevel.ToString(),
                VerificationStatus = d.VerificationStatus.ToString(),
                TrustedUntil = d.TrustedUntil, LastSeenAt = d.LastSeenAt
            }).ToListAsync(ct);

    public async Task RevokeAsync(Guid userId, Guid deviceRecordId, CancellationToken ct = default)
    {
        var device = await db.TrustedDevices.FirstOrDefaultAsync(d => d.Id == deviceRecordId && d.UserId == userId && !d.IsDeleted, ct)
            ?? throw new InvalidOperationException("Device not found.");
        device.RevokedAt = DateTime.UtcNow;
        device.VerificationStatus = DeviceVerificationStatus.Revoked;
        device.IsDeleted = true;
        await events.RecordAsync("DeviceRemoved", SecurityEventSeverity.Warning, "Device revoked", userId, deviceId: device.DeviceId, ct: ct);
        await events.WriteOutboxAsync("DeviceRevoked", device.Id, new { userId, device.DeviceId }, ct);
        await db.SaveChangesAsync(ct);
    }
}

public class RiskScoringService(IdentityDbContext db, ISecurityPolicyService policyService, ISecurityEventService events, UserManager<AppUser> userManager) : IRiskScoringService
{
    public async Task<UserRiskScoreDto> CalculateAndPersistAsync(Guid userId, CancellationToken ct = default)
    {
        var policy = await policyService.GetEffectivePolicyAsync(ct);
        var since = DateTime.UtcNow.AddHours(-24);
        var failedLogins = await db.LoginAttempts.CountAsync(a => a.UserId == userId && !a.Success && a.CreatedAt >= since, ct);
        var unknownDevice = !await db.TrustedDevices.AnyAsync(d => d.UserId == userId && !d.IsDeleted && d.RevokedAt == null && d.TrustedUntil > DateTime.UtcNow, ct);
        var user = await userManager.FindByIdAsync(userId.ToString());
        var mfaDisabled = user != null && !await userManager.GetTwoFactorEnabledAsync(user);
        var passwordAgeDays = user?.PasswordChangedAt.HasValue == true
            ? (DateTime.UtcNow - user.PasswordChangedAt!.Value).TotalDays
            : (DateTime.UtcNow - (user?.CreatedAt ?? DateTime.UtcNow)).TotalDays;
        var suspiciousSessions = await db.UserSessions.CountAsync(s => s.UserId == userId && s.IsSuspicious && s.Status == SessionStatus.Active, ct);

        var factors = new List<object>();
        var score = 0;
        if (failedLogins > 0) { score += Math.Min(30, failedLogins * 10); factors.Add(new { code = "FailedLogins24h", value = failedLogins }); }
        if (unknownDevice) { score += 20; factors.Add(new { code = "UnknownDevice", value = true }); }
        if (mfaDisabled) { score += 15; factors.Add(new { code = "MfaDisabled", value = true }); }
        if (passwordAgeDays > (policy.PasswordMaxAgeDays ?? 90)) { score += 15; factors.Add(new { code = "PasswordAge", value = passwordAgeDays }); }
        if (suspiciousSessions > 0) { score += Math.Min(20, suspiciousSessions * 10); factors.Add(new { code = "SuspiciousSessions", value = suspiciousSessions }); }
        score = Math.Min(100, score);

        var level = ScoreToLevel(score, policy);
        var factorsJson = JsonSerializer.Serialize(factors);
        var existing = await db.UserRiskScores.FirstOrDefaultAsync(r => r.UserId == userId, ct);
        if (existing != null)
        {
            var prevLevel = existing.Level;
            existing.Score = score;
            existing.Level = level;
            existing.FactorsJson = factorsJson;
            existing.CalculatedAt = DateTime.UtcNow;
            if (prevLevel != level)
                await events.WriteOutboxAsync("RiskScoreChanged", userId, new { userId, score, level = level.ToString() }, ct);
        }
        else
        {
            db.UserRiskScores.Add(new UserRiskScore
            {
                Id = Guid.NewGuid(), UserId = userId, Score = score, Level = level,
                FactorsJson = factorsJson, CalculatedAt = DateTime.UtcNow
            });
            await events.WriteOutboxAsync("RiskScoreChanged", userId, new { userId, score, level = level.ToString() }, ct);
        }
        await db.SaveChangesAsync(ct);
        return new UserRiskScoreDto { UserId = userId, Score = score, Level = level.ToString(), FactorsJson = factorsJson, CalculatedAt = DateTime.UtcNow };
    }

    public async Task<UserRiskScoreDto?> GetLatestAsync(Guid userId, CancellationToken ct = default)
    {
        var r = await db.UserRiskScores.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId, ct);
        return r == null ? null : new UserRiskScoreDto { UserId = r.UserId, Score = r.Score, Level = r.Level.ToString(), FactorsJson = r.FactorsJson, CalculatedAt = r.CalculatedAt };
    }

    public RiskLevel ScoreToLevel(int score, UserSecurityPolicy policy)
    {
        if (score >= policy.RiskThresholdHigh) return RiskLevel.Critical;
        if (score >= policy.RiskThresholdMedium) return RiskLevel.High;
        if (score >= policy.RiskThresholdMedium / 2) return RiskLevel.Medium;
        return RiskLevel.Low;
    }
}

public class ThreatDetectionService(
    IdentityDbContext db,
    ISecurityEventService events,
    ISecurityPolicyService policyService,
    IRiskScoringService riskScoring) : IThreatDetectionService
{
    public async Task EvaluateLoginAsync(Guid userId, string? ipAddress, string? geoLocation, Guid? sessionId, CancellationToken ct = default)
    {
        var policy = await policyService.GetEffectivePolicyAsync(ct);
        var recentSessions = await db.UserSessions.AsNoTracking()
            .Where(s => s.UserId == userId && s.Status == SessionStatus.Active && s.IPAddress != null)
            .OrderByDescending(s => s.LastActivityAt ?? s.CreatedAt)
            .Take(5)
            .ToListAsync(ct);

        if (recentSessions.Count > 1 && !string.IsNullOrEmpty(geoLocation) && !string.IsNullOrEmpty(ipAddress))
        {
            var distinctIps = recentSessions.Select(s => s.IPAddress).Distinct().Count();
            if (distinctIps >= 2)
            {
                await events.RecordAsync("ConcurrentLogin", SecurityEventSeverity.Warning, "Concurrent sessions from multiple locations", userId, sessionId, ipAddress: ipAddress, payload: new { geoLocation }, ct: ct);
                if (sessionId.HasValue)
                {
                    var session = await db.UserSessions.FirstOrDefaultAsync(s => s.SessionId == sessionId, ct);
                    if (session != null) { session.IsSuspicious = true; }
                }
            }
        }

        var lastLogin = await db.LoginAttempts.AsNoTracking()
            .Where(a => a.UserId == userId && a.Success)
            .OrderByDescending(a => a.CreatedAt)
            .Skip(1).FirstOrDefaultAsync(ct);

        if (lastLogin != null && geoLocation != null && lastLogin.IPAddress != ipAddress)
        {
            var elapsed = (DateTime.UtcNow - lastLogin.CreatedAt).TotalHours;
            if (elapsed > 0 && elapsed < 2)
            {
                await events.RecordAsync("ImpossibleTravel", SecurityEventSeverity.High, "Login from distant location in short time", userId, sessionId, ipAddress: ipAddress, ct: ct);
                await events.WriteOutboxAsync("ThreatDetected", userId, new { rule = "ImpossibleTravel", userId }, ct);
                db.SecurityIncidents.Add(new SecurityIncident
                {
                    Id = Guid.NewGuid(), IncidentType = "ImpossibleTravel", Status = SecurityIncidentStatus.Open,
                    UserId = userId, Summary = "Possible impossible travel detected", CreatedAt = DateTime.UtcNow
                });
            }
        }

        await riskScoring.CalculateAndPersistAsync(userId, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task EvaluateRefreshReuseAsync(Guid userId, Guid sessionId, CancellationToken ct = default)
    {
        await events.RecordAsync("JwtReuse", SecurityEventSeverity.Critical, "Refresh token reuse detected", userId, sessionId, ct: ct);
        await events.WriteOutboxAsync("SessionCompromised", sessionId, new { userId, sessionId }, ct);
        await events.WriteOutboxAsync("AccountCompromised", userId, new { userId, reason = "RefreshReuse" }, ct);
        db.SecurityIncidents.Add(new SecurityIncident
        {
            Id = Guid.NewGuid(), IncidentType = "SessionCompromised", Status = SecurityIncidentStatus.Open,
            UserId = userId, Summary = "Refresh token reuse — session family revoked", CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }
}

public class AdaptiveAuthenticationService(
    ITrustedDeviceService devices,
    IRiskScoringService riskScoring,
    ISecurityPolicyService policyService) : IAdaptiveAuthenticationService
{
    public async Task<AdaptiveAuthDecision> EvaluateLoginAsync(AdaptiveAuthContext context, CancellationToken ct = default)
    {
        var policy = await policyService.GetEffectivePolicyAsync(ct);
        var risk = await riskScoring.CalculateAndPersistAsync(context.UserId, ct);
        var trusted = !string.IsNullOrEmpty(context.DeviceId) && await devices.IsTrustedAsync(context.UserId, context.DeviceId, ct);

        if (risk.Level == RiskLevel.Critical.ToString())
            return AdaptiveAuthDecision.Block;

        if (!trusted && policy.RequireMfaOnUnknownDevice)
            return AdaptiveAuthDecision.StepUpMfa;

        if (Enum.TryParse<RiskLevel>(risk.Level, out var level) && level >= RiskLevel.High)
            return AdaptiveAuthDecision.StepUpMfa;

        return AdaptiveAuthDecision.Allow;
    }
}

public class IdentityAuditReadService(IdentityDbContext db) : IIdentityAuditReadService
{
    public async Task<IReadOnlyList<IdentityAuditLogDto>> ListAsync(DateTime? from, DateTime? to, int take = 200, CancellationToken ct = default)
    {
        var query = db.IdentityAuditLogs.AsNoTracking();
        if (from.HasValue) query = query.Where(l => l.OccurredAt >= from.Value);
        if (to.HasValue) query = query.Where(l => l.OccurredAt <= to.Value);
        return await query
            .OrderByDescending(l => l.OccurredAt)
            .Take(take)
            .Select(l => new IdentityAuditLogDto
            {
                Id = l.Id,
                OccurredAt = l.OccurredAt,
                ActorUserId = l.ActorUserId,
                TargetUserId = l.TargetUserId,
                EventCode = l.EventCode,
                Action = l.Action,
                OldValue = l.OldValue,
                NewValue = l.NewValue,
                IPAddress = l.IPAddress,
                CorrelationId = l.CorrelationId,
                Outcome = l.Outcome,
                Reason = l.Reason,
            })
            .ToListAsync(ct);
    }
}

public class SecurityTimelineService(IdentityDbContext db) : ISecurityTimelineService
{
    public async Task<IReadOnlyList<SecurityTimelineDto>> GetTimelineAsync(Guid? userId, int take = 100, CancellationToken ct = default)
    {
        var events = db.SecurityEvents.AsNoTracking().Where(e => !userId.HasValue || e.UserId == userId)
            .Select(e => new SecurityTimelineDto { Id = e.Id, Source = "SecurityEvent", EventType = e.EventType, Summary = e.Summary, OccurredAt = e.OccurredAt });
        var history = db.SessionHistories.AsNoTracking().Where(h => !userId.HasValue || h.UserId == userId)
            .Select(h => new SecurityTimelineDto { Id = h.Id, Source = "SessionHistory", EventType = h.Action.ToString(), Summary = h.Detail ?? h.Action.ToString(), OccurredAt = h.OccurredAt });
        var logins = db.LoginAttempts.AsNoTracking().Where(l => !userId.HasValue || l.UserId == userId)
            .Select(l => new SecurityTimelineDto { Id = l.Id, Source = "LoginAttempt", EventType = l.Success ? "LoginSuccess" : "LoginFailure", Summary = l.FailureReason ?? "Login", OccurredAt = l.CreatedAt });

        return await events.Concat(history).Concat(logins)
            .OrderByDescending(t => t.OccurredAt).Take(take).ToListAsync(ct);
    }
}

public class SecurityDashboardService(IdentityDbContext db, UserManager<AppUser> userManager) : ISecurityDashboardService
{
    public async Task<SecurityDashboardDto> GetSummaryAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var since = now.AddHours(-24);
        var users = await userManager.Users.CountAsync(ct);
        var mfaUsers = 0;
        foreach (var u in await userManager.Users.Take(500).ToListAsync(ct))
        {
            if (await userManager.GetTwoFactorEnabledAsync(u)) mfaUsers++;
        }
        return new SecurityDashboardDto
        {
            ActiveSessions = await db.UserSessions.CountAsync(s => s.Status == SessionStatus.Active && s.ExpiresAt > now, ct),
            LockedAccounts = await userManager.Users.CountAsync(u => u.LockoutEnd.HasValue && u.LockoutEnd > now, ct),
            HighRiskUsers = await db.UserRiskScores.CountAsync(r => r.Level >= RiskLevel.High, ct),
            FailedLogins24h = await db.LoginAttempts.CountAsync(a => !a.Success && a.CreatedAt >= since, ct),
            MfaEnabledUsers = mfaUsers,
            TotalUsers = users,
            TrustedDevices = await db.TrustedDevices.CountAsync(d => !d.IsDeleted && d.RevokedAt == null && d.TrustedUntil > now, ct),
            OpenIncidents = await db.SecurityIncidents.CountAsync(i => !i.IsDeleted && i.Status == SecurityIncidentStatus.Open, ct)
        };
    }
}

public class SecurityPolicyAdminService(IdentityDbContext db, ISecurityEventService events) : ISecurityPolicyAdminService
{
    public async Task<UserSecurityPolicy> UpdatePolicyAsync(UpdateSecurityPolicyRequest request, CancellationToken ct = default)
    {
        var policy = await db.UserSecurityPolicies.FirstOrDefaultAsync(p => p.IsActive, ct)
            ?? throw new InvalidOperationException("No active policy.");
        if (request.DeviceTrustDurationDays.HasValue) policy.DeviceTrustDurationDays = request.DeviceTrustDurationDays.Value;
        if (request.RiskThresholdMedium.HasValue) policy.RiskThresholdMedium = request.RiskThresholdMedium.Value;
        if (request.RiskThresholdHigh.HasValue) policy.RiskThresholdHigh = request.RiskThresholdHigh.Value;
        if (request.ImpossibleTravelMaxKmPerHour.HasValue) policy.ImpossibleTravelMaxKmPerHour = request.ImpossibleTravelMaxKmPerHour.Value;
        if (request.RequireMfaOnUnknownDevice.HasValue) policy.RequireMfaOnUnknownDevice = request.RequireMfaOnUnknownDevice.Value;
        if (request.SessionIdleTimeoutMinutes.HasValue) policy.SessionIdleTimeoutMinutes = request.SessionIdleTimeoutMinutes.Value;
        if (request.EmergencyAccessEnabled.HasValue) policy.EmergencyAccessEnabled = request.EmergencyAccessEnabled.Value;
        if (request.MaxConcurrentSessions.HasValue) policy.MaxConcurrentSessions = request.MaxConcurrentSessions.Value;
        policy.UpdatedAt = DateTime.UtcNow;
        await events.WriteOutboxAsync("PolicyChanged", policy.Id, new { policy.Id }, ct);
        await db.SaveChangesAsync(ct);
        return policy;
    }
}
