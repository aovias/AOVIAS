namespace ComplianceService.Application.Contracts;

/// <summary>
/// Creates or links remediation tasks when deficiencies are identified.
/// Idempotent — never duplicates open tasks for the same deficiency.
/// </summary>
public interface IDeficiencyRemediationTaskService
{
    /// <summary>
    /// Ensures an open remediation task exists for the deficiency.
    /// Returns the task id when created or linked; null when deficiency not found.
    /// </summary>
    Task<Guid?> EnsureRemediationTaskAsync(
        Guid deficiencyId,
        CancellationToken cancellationToken = default,
        bool runAutoRouting = true);
}
