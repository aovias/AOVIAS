using Serilog;
using Serilog.Events;

namespace Shared.Logging.Audit;

/// <inheritdoc />
public sealed class AuditLogger : IAuditLogger
{
    public void Record(AuditEvent auditEvent)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);

        // Tag the event so the conditional audit sink picks it up, and stamp the
        // standard classification properties. Ambient correlation / identity
        // properties (CorrelationId, UserId, ...) come from the LogContext.
        var logger = Log
            .ForContext(LoggingConstants.Properties.LogType, LoggingConstants.LogTypes.Audit)
            .ForContext(LoggingConstants.Properties.Category, LoggingConstants.Categories.Audit)
            .ForContext(LoggingConstants.Properties.EventCode, auditEvent.EventCode)
            .ForContext("Action", auditEvent.Action)
            .ForContext("Outcome", auditEvent.Outcome.ToString())
            .ForContext("TargetType", auditEvent.TargetType)
            .ForContext("TargetId", auditEvent.TargetId)
            .ForContext("ActorId", auditEvent.ActorId)
            .ForContext("Reason", auditEvent.Reason)
            .ForContext("AuditDetails", auditEvent.Details, destructureObjects: true);

        var level = auditEvent.Outcome == AuditOutcome.Failure ? LogEventLevel.Warning : LogEventLevel.Information;
        logger.Write(level, "AUDIT {Action} {Outcome} ({EventCode})", auditEvent.Action, auditEvent.Outcome, auditEvent.EventCode);
    }

    public void Success(string eventCode, string action, string? targetType = null, string? targetId = null, string? actorId = null, object? details = null)
        => Record(new AuditEvent
        {
            EventCode = eventCode,
            Action = action,
            Outcome = AuditOutcome.Success,
            TargetType = targetType,
            TargetId = targetId,
            ActorId = actorId,
            Details = details
        });

    public void Failure(string eventCode, string action, string? reason = null, string? targetType = null, string? targetId = null, string? actorId = null, object? details = null)
        => Record(new AuditEvent
        {
            EventCode = eventCode,
            Action = action,
            Outcome = AuditOutcome.Failure,
            Reason = reason,
            TargetType = targetType,
            TargetId = targetId,
            ActorId = actorId,
            Details = details
        });
}
