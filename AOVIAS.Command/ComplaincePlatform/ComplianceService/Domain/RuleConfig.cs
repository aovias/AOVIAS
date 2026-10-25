using Shared.Infrastructure.Domain;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Domain
{
    public class RuleConfig : BaseEntity
    {
        public Guid FrameworkId { get; set; }
        public string Name { get; set; }               // e.g., NABH Final 2024
        public OperationalMode OperationalMode { get; set; }
        public string Version { get; set; }
        public bool IsActive { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public string RulesJson { get; set; }          // JSON definition for rule engine
    }
}
