using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain
{
    public class ObjectiveElementScore : BaseEntity
    {
        public Guid ObjectiveElementId { get; set; }
        public ObjectiveElement ObjectiveElement { get; set; }

        public Guid JourneyId { get; set; }
        public Guid AssessorId { get; set; }

        public int Score { get; set; } // 1–5
        public string Rationale { get; set; }
        public decimal EvidenceSampleCompliancePercent { get; set; }

        public bool IsDocumentationInadequate { get; set; }
        public bool DowngradeApplied { get; set; }
        public string? DowngradeReason { get; set; }

        public bool IsApplicable { get; set; } = true;
        public string? NaReason { get; set; }
        public Guid? NaApprovedBy { get; set; }

        public DateTime AssessedAt { get; set; }
    }
}

