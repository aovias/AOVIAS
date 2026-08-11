using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain;

/// <summary>
/// Immutable audit record for evidence review decisions (healthcare compliance trail).
/// </summary>
public class EvidenceReviewAuditEntry : BaseEntity
{
    public Guid EvidenceId { get; set; }
    public Guid ObjectiveElementId { get; set; }
    public Guid HospitalId { get; set; }
    public Guid FrameworkId { get; set; }
    public Guid ReviewerId { get; set; }
    public string? ReviewerRole { get; set; }
    public DateTime ReviewedAt { get; set; }
    public string Decision { get; set; } = string.Empty;
    public EvidenceVerificationStatus PreviousStatus { get; set; }
    public EvidenceVerificationStatus NewStatus { get; set; }
    public string? Comment { get; set; }
    public string? Recommendation { get; set; }
    public int EvidenceVersion { get; set; }
}
