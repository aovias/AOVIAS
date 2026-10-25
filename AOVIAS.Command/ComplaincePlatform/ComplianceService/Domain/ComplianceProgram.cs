using Shared.Infrastructure.Domain;
using Shared.Infrastructure.DTO;

namespace ComplianceService.Domain
{
    public class ComplianceProgram : PlatformEntity
    {
        public Guid HospitalId { get; set; }
        public Guid ComplianceFrameworkId { get; set; }
        public ComplianceFramework ComplianceFramework { get; set; }
        public string Name { get; set; }
        public ExplainabilityDto? Explainability { get; set; }
        // Add other fields as needed
    }
}
