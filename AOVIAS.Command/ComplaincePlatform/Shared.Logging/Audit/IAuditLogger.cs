namespace Shared.Logging.Audit;

/// <summary>
/// Writes tamper-evident security / audit events to the dedicated audit log stream
/// (<c>_audit/{service}-audit-*.clef</c>). Events also flow to the operational log.
///
/// This answers "who did what, when, from where, and with what outcome" for security
/// events. A durable DB-backed audit store remains a separate, larger workstream.
/// </summary>
public interface IAuditLogger
{
    /// <summary>Records a structured audit event.</summary>
    void Record(AuditEvent auditEvent);

    /// <summary>Convenience: a successful, security-relevant action.</summary>
    void Success(string eventCode, string action, string? targetType = null, string? targetId = null, string? actorId = null, object? details = null);

    /// <summary>Convenience: a failed / denied security-relevant action.</summary>
    void Failure(string eventCode, string action, string? reason = null, string? targetType = null, string? targetId = null, string? actorId = null, object? details = null);
}

/// <summary>Outcome of an audited action.</summary>
public enum AuditOutcome
{
    Success,
    Failure
}

/// <summary>Immutable description of a single audit event.</summary>
public sealed class AuditEvent
{
    public required string EventCode { get; init; }
    public required string Action { get; init; }
    public AuditOutcome Outcome { get; init; } = AuditOutcome.Success;

    /// <summary>Type of the affected entity (e.g. "User", "Role", "ComplianceJourney").</summary>
    public string? TargetType { get; init; }
    public string? TargetId { get; init; }

    /// <summary>Identifier of the actor performing the action (user id / service account).</summary>
    public string? ActorId { get; init; }

    /// <summary>Optional reason, used primarily for failures.</summary>
    public string? Reason { get; init; }

    /// <summary>Optional structured payload (e.g. before/after values). Avoid PHI / secrets.</summary>
    public object? Details { get; init; }
}
