namespace ComplianceService.Domain.Copilot;

/// <summary>
/// Sprint 13E — immutable activity record for Copilot engagement tracking.
/// Read-only feed; never updated after creation.
/// </summary>
public sealed class CopilotActivityEvent
{
    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string? Persona { get; private set; }
    public string? ActorId { get; private set; }
    public string? ActorEmail { get; private set; }
    public string? EntityType { get; private set; }
    public Guid? EntityId { get; private set; }
    public string? Notes { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }

    private CopilotActivityEvent() { }

    public static class EventTypes
    {
        public const string RecommendationGenerated = "RecommendationGenerated";
        public const string DraftCreated = "DraftCreated";
        public const string ProposalApproved = "ProposalApproved";
        public const string ProposalExecuted = "ProposalExecuted";
        public const string PersonaChanged = "PersonaChanged";
        public const string WorkspaceViewed = "WorkspaceViewed";
        public const string RecommendationViewed = "RecommendationViewed";
    }

    public static CopilotActivityEvent Record(
        Guid organizationId,
        string eventType,
        string? persona = null,
        string? actorId = null,
        string? actorEmail = null,
        string? entityType = null,
        Guid? entityId = null,
        string? notes = null) =>
        new()
        {
            Id             = Guid.NewGuid(),
            OrganizationId = organizationId,
            EventType      = eventType,
            Persona        = persona,
            ActorId        = actorId,
            ActorEmail     = actorEmail,
            EntityType     = entityType,
            EntityId       = entityId,
            Notes          = notes,
            OccurredAtUtc  = DateTime.UtcNow,
        };
}
