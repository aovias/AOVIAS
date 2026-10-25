using ComplianceService.Domain;

namespace ComplianceService.Application.ActionPlans
{
    public interface IActionPlanService
    {
        Task<ActionPlan?> EnsureForLowScoreAsync(
            Guid objectiveElementId,
            Guid journeyId,
            int score,
            Guid createdBy,
            string? defaultRootCause,
            string? defaultCorrectiveAction,
            DateTime? dueDate,
            CancellationToken cancellationToken = default);
    }
}

