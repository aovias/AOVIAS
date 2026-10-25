using ComplianceService.Application.DTOs.ControlCenter;

namespace ComplianceService.Application.Contracts;

/// <summary>
/// Hosts the Control Center intelligence engine, risk-scoring and audit-confidence formulas.
/// Reuses <see cref="IComplianceScoreService"/> for compliance percentages (single source of truth).
/// </summary>
public interface IControlCenterAggregationService
{
    /// <summary>
    /// Computes live signals (alerts + activity events) from the dataset and persists them
    /// idempotently so the new tables reflect current posture. Safe to call on every refresh.
    /// </summary>
    Task PopulateSignalsAsync(ControlCenterScope scope, ControlCenterDataset dataset, CancellationToken cancellationToken = default);

    Task<ControlCenterOverviewDto> BuildOverviewAsync(ControlCenterScope scope, ControlCenterDataset dataset, CancellationToken cancellationToken = default);
}
