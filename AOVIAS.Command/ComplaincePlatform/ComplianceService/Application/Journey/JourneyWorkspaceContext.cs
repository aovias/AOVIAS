using ComplianceService.Application.Aggregation.Models;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.Journey;

/// <summary>
/// Shared loaded state for journey workspace projections (session, dashboard, chapters, standards, OE detail).
/// </summary>
internal sealed class JourneyWorkspaceContext
{
    public required ComplianceJourney Journey { get; init; }
    public required ComplianceFramework Framework { get; init; }
    public required OperationalMode ResolvedMode { get; init; }
    public required FrameworkAggregateResult Aggregate { get; init; }
    public required List<ComplianceChapter> Chapters { get; init; }
    public required List<ComplianceStandard> Standards { get; init; }
    public required List<ObjectiveElement> ObjectiveElements { get; init; }
    public required Dictionary<Guid, ObjectiveElementScore> LatestByOe { get; init; }
    public required List<Finding> Findings { get; init; }
    public required List<ObjectiveElementObservation> Observations { get; init; }
    public required List<ObjectiveElementEvidence> EvidenceLinks { get; init; }
    public required List<ActionPlan> ActionPlans { get; init; }
    public required Dictionary<Guid, ObjectiveElement> OeById { get; init; }

    /// <summary>Per OE: OE execution wizard row in <c>ObjectiveJourneys</c> (draft / in progress / completed).</summary>
    public required IReadOnlyDictionary<Guid, ObjectiveJourneyStatus> OeWizardStatusByObjectiveElementId { get; init; }
}
