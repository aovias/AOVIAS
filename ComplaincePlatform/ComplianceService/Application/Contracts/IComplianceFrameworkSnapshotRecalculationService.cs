namespace ComplianceService.Application.Contracts;

/// <summary>
/// Persists a framework-level compliance snapshot for a hospital, tied to the active <see cref="Domain.ComplianceJourney"/>.
/// </summary>
public interface IComplianceFrameworkSnapshotRecalculationService
{
    /// <summary>
    /// Computes aggregation + decision and appends a <see cref="Domain.ComplianceSnapshot"/> row.
    /// </summary>
    /// <returns>True if a journey existed and a snapshot row was written; false if skipped (no journey or invalid ids).</returns>
    Task<bool> RecalculateAsync(Guid hospitalId, Guid frameworkId, CancellationToken cancellationToken = default);
}
