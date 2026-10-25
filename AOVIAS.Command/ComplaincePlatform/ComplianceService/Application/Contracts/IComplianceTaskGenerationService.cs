using System.Threading;
using System.Threading.Tasks;

namespace ComplianceService.Application.Contracts;

/// <summary>
/// Application service responsible for generating ComplianceTask rows
/// for a given ComplianceJourney (typically immediately after journey creation).
/// </summary>
public interface IComplianceTaskGenerationService
{
    /// <summary>
    /// Generates compliance tasks for all applicable objective elements under the journey's framework (NABH hierarchy).
    /// Idempotent: if tasks already exist for the hospital+framework, implementation should
    /// either skip or ensure no duplicates are created.
    /// </summary>
    /// <param name="journeyId">The identifier of the ComplianceJourney.</param>
    /// <param name="creatingUserId">User to assign to newly created tasks (and to backfill tasks with an empty assignee).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task GenerateTasksForJourneyAsync(
        Guid journeyId,
        Guid creatingUserId,
        CancellationToken cancellationToken = default);
}

