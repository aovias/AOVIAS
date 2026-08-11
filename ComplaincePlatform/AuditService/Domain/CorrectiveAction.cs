using Shared.Infrastructure.Domain;

namespace AuditService.Domain
{
    public class CorrectiveAction : BaseEntity
    {
        public Guid FindingId { get; set; }
        public string ActionPlan { get; set; }
        public DateTime DueDate { get; set; }
        public string Status { get; set; }
    }
}
