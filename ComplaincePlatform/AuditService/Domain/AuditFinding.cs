using Shared.Infrastructure.Domain;
using Shared.Infrastructure.DTO;

namespace AuditService.Domain
{
    public class AuditFinding : BaseEntity
    {
        public Guid AuditSessionId { get; set; }
        public string ControlCode { get; set; }
        public string Severity { get; set; }
        public string Description { get; set; }
        public ExplainabilityDto? Explainability { get; set; }
    }
}
