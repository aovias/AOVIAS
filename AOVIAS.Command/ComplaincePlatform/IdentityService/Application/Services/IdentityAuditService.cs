using IdentityService.Application.Contracts;
using IdentityService.Domain.Entities;
using IdentityService.Domain.Enums;
using IdentityService.Infrastructure;
using IdentityService.Infrastructure.Security;
using Shared.Logging;
using Shared.Logging.Audit;

namespace IdentityService.Application.Services;

public class IdentityAuditService : IIdentityAuditService
{
    private readonly IdentityDbContext _db;
    private readonly IAuditLogger _auditLogger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public IdentityAuditService(
        IdentityDbContext db,
        IAuditLogger auditLogger,
        IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _auditLogger = auditLogger;
        _httpContextAccessor = httpContextAccessor;
    }

    public Task RecordSuccessAsync(
        string eventCode,
        string action,
        Guid? actorUserId = null,
        Guid? targetUserId = null,
        string? oldValue = null,
        string? newValue = null,
        Guid? sessionId = null,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        return RecordAsync(eventCode, action, "Success", reason, actorUserId, targetUserId, oldValue, newValue, sessionId, cancellationToken, success: true);
    }

    public Task RecordFailureAsync(
        string eventCode,
        string action,
        string reason,
        Guid? actorUserId = null,
        Guid? targetUserId = null,
        Guid? sessionId = null,
        CancellationToken cancellationToken = default)
    {
        return RecordAsync(eventCode, action, "Failure", reason, actorUserId, targetUserId, null, null, sessionId, cancellationToken, success: false);
    }

    private async Task RecordAsync(
        string eventCode,
        string action,
        string outcome,
        string? reason,
        Guid? actorUserId,
        Guid? targetUserId,
        string? oldValue,
        string? newValue,
        Guid? sessionId,
        CancellationToken cancellationToken,
        bool success)
    {
        var http = _httpContextAccessor.HttpContext;
        var userAgent = ClientContextParser.GetUserAgent(http);

        _db.IdentityAuditLogs.Add(new IdentityAuditLog
        {
            Id = Guid.NewGuid(),
            OccurredAt = DateTime.UtcNow,
            ActorUserId = actorUserId,
            TargetUserId = targetUserId,
            EventCode = eventCode,
            Action = action,
            OldValue = oldValue,
            NewValue = newValue,
            IPAddress = ClientContextParser.GetIpAddress(http),
            UserAgent = userAgent,
            OperatingSystem = ClientContextParser.ParseOperatingSystem(userAgent),
            CorrelationId = ClientContextParser.GetCorrelationId(http),
            SessionId = sessionId,
            Outcome = outcome,
            Reason = reason
        });

        var severity = success ? SecurityEventSeverity.Info : SecurityEventSeverity.Warning;
        if (eventCode.Contains("Failed", StringComparison.OrdinalIgnoreCase) || outcome == "Failure")
            severity = SecurityEventSeverity.Warning;

        _db.SecurityEvents.Add(new SecurityEvent
        {
            Id = Guid.NewGuid(),
            EventType = eventCode,
            Severity = severity,
            UserId = targetUserId ?? actorUserId,
            SessionId = sessionId,
            Summary = $"{action}: {outcome}" + (reason != null ? $" — {reason}" : string.Empty),
            IPAddress = ClientContextParser.GetIpAddress(http),
            OccurredAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);

        if (success)
        {
            _auditLogger.Success(eventCode, action,
                targetType: "User",
                targetId: targetUserId?.ToString() ?? actorUserId?.ToString(),
                actorId: actorUserId?.ToString());
        }
        else
        {
            _auditLogger.Failure(eventCode, action, reason: reason,
                targetType: "User",
                targetId: targetUserId?.ToString(),
                actorId: actorUserId?.ToString());
        }
    }
}
