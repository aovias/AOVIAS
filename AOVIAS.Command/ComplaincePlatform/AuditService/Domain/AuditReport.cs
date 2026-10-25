using Shared.Infrastructure.Domain;

namespace AuditService.Domain
{
    public class AuditReport : BaseEntity
    {
        public Guid AuditSessionId { get; set; }
        public string Summary { get; set; }
        public DateTime GeneratedAt { get; set; }
    }
}
