using Shared.Infrastructure.Domain;
using Shared.Infrastructure.DTO;

namespace ComplianceService.Domain
{
    public class Control : BaseEntity
    {
        public Guid ProgramId { get; set; }
        public Guid? ObjectiveElementId { get; set; }
        public string Code { get; set; }  // e.g. NABH-01
        public string Description { get; set; }
        public string Phase { get; set; } = "Readiness";
        public ExplainabilityDto? Explainability { get; set; }
    }
}
