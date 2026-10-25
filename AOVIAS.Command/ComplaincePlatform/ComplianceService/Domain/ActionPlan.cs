using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain
{
    public enum ActionPlanStatus
    {
        Open = 0,
        InProgress = 1,
        Completed = 2,
        Cancelled = 3
    }

    public class ActionPlan : BaseEntity
    {
        public Guid ObjectiveElementId { get; set; }
        public ObjectiveElement ObjectiveElement { get; set; }

        public string RootCause { get; set; }
        public string CorrectiveAction { get; set; }
        public Guid OwnerId { get; set; }
        public DateTime DueDate { get; set; }
        public ActionPlanStatus Status { get; set; }

        public Guid? LinkedTaskId { get; set; }

        public Guid CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

