using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain;

/// <summary>
/// Free-text observation attached to an OE within a journey (minor/major for audit trail).
/// </summary>
public class ObjectiveElementObservation : BaseEntity
{
    public Guid ComplianceJourneyId { get; set; }
    public ComplianceJourney ComplianceJourney { get; set; } = null!;

    public Guid ObjectiveElementId { get; set; }
    public ObjectiveElement ObjectiveElement { get; set; } = null!;

    /// <summary>minor | major</summary>
    public string Severity { get; set; } = "minor";

    public string Text { get; set; } = string.Empty;

    public Guid CreatedByUserId { get; set; }
}
