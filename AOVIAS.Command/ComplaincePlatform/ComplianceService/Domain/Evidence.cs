using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain
{
    public class Evidence : BaseEntity
    {
        public Guid TaskId { get; set; }
        public ComplianceTask Task { get; set; }

        public Guid HospitalId { get; set; }
        public Guid FrameworkId { get; set; }

        public string EvidenceType { get; set; }

        public enum EvidenceArtifactType
        {
            Document = 0,
            Process = 1,
            Outcome = 2
        }

        public enum EvidenceLifecycleStatus
        {
            Draft = 0,
            Submitted = 1,
            Verified = 2,
            Rejected = 3
        }

        public EvidenceArtifactType ArtifactType { get; set; } = EvidenceArtifactType.Document;
        public EvidenceLifecycleStatus LifecycleStatus { get; set; } = EvidenceLifecycleStatus.Submitted;

        public string? Title { get; set; }
        public string? Description { get; set; }
        public string Source { get; set; } = "Manual";
        public string? Department { get; set; }
        public string? Tags { get; set; }
        public int CurrentVersion { get; set; } = 1;
        public string FileUrl { get; set; }
        public string Hash { get; set; } // Integrity check
        public string UploadedBy { get; set; } // User identifier
        public DateTime UploadedOn { get; set; }

        /// <summary>Optional validity end for audit / renewal tracking.</summary>
        public DateTime? ExpiresAt { get; set; }

        public string Metadata { get; set; } // JSON
    }
}
