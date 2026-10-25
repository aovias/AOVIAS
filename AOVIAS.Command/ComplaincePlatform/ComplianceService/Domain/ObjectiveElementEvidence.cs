using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain
{
    public enum EvidenceVerificationStatus
    {
        /// <summary>Legacy: awaiting reviewer action (same as PendingReview).</summary>
        Pending = 0,
        /// <summary>Approved / verified evidence.</summary>
        Verified = 1,
        Rejected = 2,
        Draft = 3,
        Submitted = 4,
        NeedsMoreEvidence = 5
    }

    public class ObjectiveElementEvidence : BaseEntity
    {
        public Guid ObjectiveElementId { get; set; }
        public ObjectiveElement ObjectiveElement { get; set; }

        public Guid EvidenceId { get; set; }
        public Evidence Evidence { get; set; }

        public Guid LinkedBy { get; set; }
        public DateTime LinkedAt { get; set; }

        public Guid? VerifiedBy { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public EvidenceVerificationStatus VerificationStatus { get; set; }
        public string? Remarks { get; set; }
        /// <summary>Review comment captured during approval/rejection workflow.</summary>
        public string? ReviewComment { get; set; }
        /// <summary>Optional reviewer recommendation (separate from rejection comment).</summary>
        public string? ReviewRecommendation { get; set; }
    }
}

