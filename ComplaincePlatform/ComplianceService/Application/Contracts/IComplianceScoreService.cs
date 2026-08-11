using ComplianceService.Application.ComplianceScoring;
using OeClassification = ComplianceService.Domain.ObjectiveElementClassification;

namespace ComplianceService.Application.Contracts;

/// <summary>
/// Canonical single source of truth for compliance percentage calculations.
/// Every screen (Dashboard, Journey, Sidebar/Program Overview, Reports) must obtain its
/// compliance percentage from this service so values are identical across the platform.
/// All percentages are on a 0-100 scale and are computed live from objective-element
/// assessments via <see cref="Aggregation.IAggregationService"/> using a single, fixed
/// classification policy.
/// </summary>
public interface IComplianceScoreService
{
    /// <summary>
    /// Compliance percentage (0-100) for a specific journey of a framework.
    /// This is the atomic value all other aggregations are built from.
    /// </summary>
    Task<decimal> GetJourneyScoreAsync(Guid frameworkId, Guid journeyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Framework-level compliance result (0-100) for a hospital. Resolves the active journey
    /// for the hospital+framework deterministically and computes its score plus chapter breakdown.
    /// </summary>
    Task<ComplianceScoreResult> GetFrameworkScoreAsync(Guid hospitalId, Guid frameworkId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Hospital-level overall compliance result (0-100), a chapter-count-weighted average
    /// across every framework the hospital is enrolled in.
    /// </summary>
    Task<ComplianceScoreResult> GetHospitalScoreAsync(Guid hospitalId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Per-stage compliance percentage (0-100) for a specific journey, derived from
    /// <see cref="ObjectiveElementScore"/> rows filtered by <paramref name="stage"/>.
    /// Returns 0 when the journey has no scored OEs for that stage.
    /// </summary>
    Task<decimal> GetStageScoreAsync(Guid journeyId, OeClassification stage, CancellationToken cancellationToken = default);
}
