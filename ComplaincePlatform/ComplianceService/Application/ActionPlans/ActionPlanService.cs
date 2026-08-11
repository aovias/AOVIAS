using ComplianceService.Domain;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Application.ActionPlans
{
    public class ActionPlanService : IActionPlanService
    {
        private readonly ComplianceDbContext _db;

        public ActionPlanService(ComplianceDbContext db)
        {
            _db = db;
        }

        public async Task<ActionPlan?> EnsureForLowScoreAsync(
            Guid objectiveElementId,
            Guid journeyId,
            int score,
            Guid createdBy,
            string? defaultRootCause,
            string? defaultCorrectiveAction,
            DateTime? dueDate,
            CancellationToken cancellationToken = default)
        {
            if (score > 3)
            {
                return null;
            }

            // Idempotency: if there's an open plan for this OE+journey, return it
            var existing = await _db.ActionPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(ap => ap.ObjectiveElementId == objectiveElementId
                                           && (ap.Status == ActionPlanStatus.Open || ap.Status == ActionPlanStatus.InProgress),
                    cancellationToken);
            if (existing != null)
            {
                return existing;
            }

            var plan = new ActionPlan
            {
                Id = Guid.NewGuid(),
                ObjectiveElementId = objectiveElementId,
                RootCause = defaultRootCause ?? "To be analyzed",
                CorrectiveAction = defaultCorrectiveAction ?? "To be defined",
                OwnerId = createdBy,
                DueDate = dueDate ?? DateTime.UtcNow.AddDays(30),
                Status = ActionPlanStatus.Open,
                LinkedTaskId = null,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };

            _db.ActionPlans.Add(plan);
            await _db.SaveChangesAsync(cancellationToken);
            return plan;
        }
    }
}

