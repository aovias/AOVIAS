using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain
{
    public enum ObjectiveElementClassification
    {
        Core = 0,
        Commitment = 1,
        Achievement = 2,
        Excellence = 3
    }

    public class ObjectiveElement : PlatformEntity
    {
        private decimal _weight = 1.0m;

        public Guid ComplianceStandardId { get; set; }
        public ComplianceStandard ComplianceStandard { get; set; }

        public string Code { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }

        public ObjectiveElementClassification Classification { get; set; }
        public bool IsMandatory { get; set; }
        public bool IsScorable { get; set; } = true;
        public bool IsApplicable { get; set; } = true;
        public bool IsDocumentationRequired { get; set; } = false;
        public string? Tags { get; set; }

        /// <summary>Optional operational metadata (from master-data enrichment).</summary>
        public string? Department { get; set; }

        public string? ResponsibleRole { get; set; }

        public string? Frequency { get; set; }

        public string? EvidenceCategory { get; set; }

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

