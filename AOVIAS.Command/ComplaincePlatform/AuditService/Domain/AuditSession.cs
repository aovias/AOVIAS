using Shared.Infrastructure.Domain;
using Shared.Infrastructure.DTO;

namespace AuditService.Domain
{
    public class AuditSession : PlatformEntity
    {
        public Guid OrganizationId { get; set; }
        public Guid ComplianceProgramId { get; set; }
        public DateTime StartedAt { get; set; }
        public string Status { get; set; } // Draft, InProgress, Completed
        public ExplainabilityDto? Explainability { get; set; }
    }
}
