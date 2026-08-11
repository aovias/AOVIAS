namespace ComplianceService.Domain.Copilot;

/// <summary>
/// Immutable audit record for every state change on a <see cref="CopilotActionProposal"/>.
/// Written once; never updated or deleted.
/// Required for governance, compliance reporting, and Copilot effectiveness tracking.
/// </summary>
public class ProposalAuditEvent
{
    // ── Identity ─────────────────────────────────────────────────────────────
    public Guid Id { get; private set; }
    public Guid ProposalId { get; private set; }
    public Guid OrganizationId { get; private set; }

    // ── Event ─────────────────────────────────────────────────────────────────
    public string EventType { get; private set; } = string.Empty;
    public string? ActorId { get; private set; }
    public string? ActorEmail { get; private set; }
    public string? Notes { get; private set; }

    /// <summary>Serialized snapshot of proposal state at event time.</summary>
    public string? ContextJson { get; private set; }

    public DateTime OccurredAtUtc { get; private set; }

    // ── EF Core constructor ───────────────────────────────────────────────────
    private ProposalAuditEvent() { }

    // ── Well-known event types ─────────────────────────────────────────────────
    public static class EventTypes
    {
        public const string Created  = "Created";
        public const string Viewed   = "Viewed";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";
        public const string Executed = "Executed";
        public const string Expired  = "Expired";
        public const string Generated = "Generated";
    }

    // ── Factory ──────────────────────────────────────────────────────────────

    public static ProposalAuditEvent Record(
        Guid proposalId,
        Guid organizationId,
        string eventType,
        string? actorId = null,
        string? actorEmail = null,
        string? notes = null,
        string? contextJson = null) =>
        new()
        {
            Id             = Guid.NewGuid(),
            ProposalId     = proposalId,
            OrganizationId = organizationId,
            EventType      = eventType,
            ActorId        = actorId,
            ActorEmail     = actorEmail,
            Notes          = notes,
            ContextJson    = contextJson,
            OccurredAtUtc  = DateTime.UtcNow,
        };
}
