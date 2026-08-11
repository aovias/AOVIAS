using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain
{
    public class ComplianceChapter : PlatformEntity
    {
        private decimal _weight = 1.0m;

        public Guid FrameworkId { get; set; }
        public ComplianceFramework Framework { get; set; }

        public string Code { get; set; }     // PRE, ICC, HRM
        public string Name { get; set; }     // Patient Rights, Infection Control
        public int Sequence { get; set; }

        /// <summary>
        /// Weight for compliance scoring (0-100 range recommended).
        /// Default value: 1.0
        /// </summary>
        public decimal Weight
        {
            get => _weight;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException("Weight cannot be negative.", nameof(value));
                }
                _weight = value;
            }
        }

        public ICollection<ComplianceStandard> ComplianceStandards { get; set; } = new List<ComplianceStandard>();
    }
}
