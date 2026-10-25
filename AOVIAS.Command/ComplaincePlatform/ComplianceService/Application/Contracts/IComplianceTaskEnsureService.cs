using ComplianceService.Domain;

namespace ComplianceService.Application.Contracts;

/// <summary>
/// Ensures a <see cref="ComplianceTask"/> exists for a hospital + objective element (journey context),
/// with a non-empty assignee, using the same regulation resolution rules as task generation / evidence flows.
/// </summary>
public interface IComplianceTaskEnsureService
{
    Task<ComplianceTask> EnsureComplianceTaskForObjectiveElementAsync(
        ComplianceJourney journey,
        Guid objectiveElementId,
        Guid assignedUserId,
        CancellationToken cancellationToken = default);
}
