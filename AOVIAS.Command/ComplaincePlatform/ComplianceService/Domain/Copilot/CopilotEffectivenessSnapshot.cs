namespace ComplianceService.Domain.Copilot;

/// <summary>
/// Sprint 13F — periodic aggregate of Copilot effectiveness metrics.
/// Derived from recommendations, drafts, proposals, and activity events.
/// Immutable once created for a snapshot date.
/// </summary>
public sealed class CopilotEffectivenessSnapshot
{
    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public DateOnly SnapshotDate { get; private set; }

    public int RecommendationsGenerated { get; private set; }
    public int RecommendationsViewed { get; private set; }
    public int RecommendationsAccepted { get; private set; }
    public int RecommendationsDismissed { get; private set; }

    public int DraftsGenerated { get; private set; }
    public int DraftsRegenerated { get; private set; }

    public int ProposalsCreated { get; private set; }
    public int ProposalsApproved { get; private set; }
    public int ProposalsRejected { get; private set; }
    public int ProposalsExecuted { get; private set; }

    public int PersonaSwitches { get; private set; }
    public int ActiveUsers { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    private CopilotEffectivenessSnapshot() { }

    public static CopilotEffectivenessSnapshot Create(
        Guid organizationId,
        DateOnly snapshotDate,
        int recommendationsGenerated,
        int recommendationsViewed,
        int recommendationsAccepted,
        int recommendationsDismissed,
        int draftsGenerated,
        int draftsRegenerated,
        int proposalsCreated,
        int proposalsApproved,
        int proposalsRejected,
        int proposalsExecuted,
        int personaSwitches,
        int activeUsers) =>
        new()
        {
            Id                      = Guid.NewGuid(),
            OrganizationId          = organizationId,
            SnapshotDate            = snapshotDate,
            RecommendationsGenerated = recommendationsGenerated,
            RecommendationsViewed   = recommendationsViewed,
            RecommendationsAccepted = recommendationsAccepted,
            RecommendationsDismissed = recommendationsDismissed,
            DraftsGenerated         = draftsGenerated,
            DraftsRegenerated         = draftsRegenerated,
            ProposalsCreated        = proposalsCreated,
            ProposalsApproved       = proposalsApproved,
            ProposalsRejected       = proposalsRejected,
            ProposalsExecuted       = proposalsExecuted,
            PersonaSwitches         = personaSwitches,
            ActiveUsers             = activeUsers,
            CreatedAtUtc            = DateTime.UtcNow,
        };
}
