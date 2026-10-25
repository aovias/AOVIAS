using Shared.Infrastructure.Domain;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Domain;

/// <summary>
/// Audit trail for compliance journey state transitions.
/// Records every state change with full context.
/// </summary>
public class ComplianceJourneyHistory : BaseEntity
{
    public Guid ComplianceJourneyId { get; private set; }
    public ComplianceJourney ComplianceJourney { get; private set; } = null!;

    public ComplianceLifecycleState OldStatus { get; private set; }
    public ComplianceLifecycleState NewStatus { get; private set; }

    public Guid ChangedBy { get; private set; }
    public DateTime ChangedAt { get; private set; }
    public string? Remarks { get; private set; }

    /// <summary>
    /// JSON snapshot of the compliance journey state at the time of transition.
    /// Stores all relevant fields for audit purposes.
    /// </summary>
    public string SnapshotJson { get; private set; } = string.Empty;

    private ComplianceJourneyHistory() { }

    public ComplianceJourneyHistory(
        Guid complianceJourneyId,
        ComplianceLifecycleState oldStatus,
        ComplianceLifecycleState newStatus,
        Guid changedBy,
        string? remarks,
        string snapshotJson)
    {
        Id = Guid.NewGuid();
        ComplianceJourneyId = complianceJourneyId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        ChangedBy = changedBy;
        ChangedAt = DateTime.UtcNow;
        Remarks = remarks;
        SnapshotJson = snapshotJson;
        CreatedAt = DateTime.UtcNow;
    }
}
