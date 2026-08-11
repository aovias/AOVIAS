using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain
{
    public class ComplianceStandard : PlatformEntity
    {
        private decimal _weight = 1.0m;

        public Guid ChapterId { get; set; }
        public ComplianceChapter Chapter { get; set; }

        public string Code { get; set; }
        public string Name { get; set; }
        public int Sequence { get; set; }

        public ICollection<ObjectiveElement> ObjectiveElements { get; set; } = new List<ObjectiveElement>();

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
    }
}

