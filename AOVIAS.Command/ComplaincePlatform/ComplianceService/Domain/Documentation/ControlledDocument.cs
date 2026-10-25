using ComplianceService.Domain.Enums;
using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain.Documentation;

/// <summary>
/// Primary aggregate root for Quality Documentation &amp; Policy Management (QDMS).
/// Policies are NOT evidence — this bounded context is independent from Evidence Library.
/// </summary>
public class ControlledDocument : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid? HospitalId { get; set; }

    public string DocumentNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public ControlledDocumentType DocumentType { get; set; } = ControlledDocumentType.Policy;
    public ControlledDocumentStatus Status { get; set; } = ControlledDocumentStatus.Draft;

    public string? Department { get; set; }
    public Guid? OwnerUserId { get; set; }
    public string? OwnerName { get; set; }
    public Guid? ReviewerUserId { get; set; }
    public string? ReviewerName { get; set; }
    public Guid? ApproverUserId { get; set; }
    public string? ApproverName { get; set; }

    public Guid? CurrentVersionId { get; set; }
    public string? CurrentVersionLabel { get; set; }

    public DateTime? EffectiveDate { get; set; }
    public DateTime? ReviewDate { get; set; }
    public DateTime? NextReviewDate { get; set; }
    /// <summary>Optional hard expiry — past this date the document cannot remain Effective.</summary>
    public DateTime? ExpiryDate { get; set; }
    public int ReviewFrequencyMonths { get; set; } = 12;
    public Guid? ReviewOwnerUserId { get; set; }
    public string? ReviewOwnerName { get; set; }

    public Guid? FrameworkId { get; set; }
    public string? MappedFramework { get; set; }
    public string? MappedChapter { get; set; }
    public string? MappedStandard { get; set; }
    public Guid? MappedObjectiveElementId { get; set; }
    public string? MappedObjectiveElement { get; set; }

    public bool IsArchived { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? LastUpdatedByUserId { get; set; }

    public ICollection<DocumentVersion> Versions { get; set; } = new List<DocumentVersion>();
    public ICollection<DocumentReview> Reviews { get; set; } = new List<DocumentReview>();
    public ICollection<DocumentAcknowledgement> Acknowledgements { get; set; } = new List<DocumentAcknowledgement>();
    public ICollection<DocumentLifecycleAuditEntry> LifecycleAudit { get; set; } = new List<DocumentLifecycleAuditEntry>();
    public ICollection<DocumentAuthoringComment> AuthoringComments { get; set; } = new List<DocumentAuthoringComment>();
    public ICollection<DocumentAuthoringCheckpoint> AuthoringCheckpoints { get; set; } = new List<DocumentAuthoringCheckpoint>();
    public ICollection<DocumentCrossReference> CrossReferences { get; set; } = new List<DocumentCrossReference>();

    public bool IsPastExpiry(DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        return ExpiryDate.HasValue && ExpiryDate.Value <= now;
    }

    /// <summary>Expired or archived controlled documents must not be mutated.</summary>
    public bool IsImmutable(DateTime? utcNow = null) =>
        IsArchived
        || Status == ControlledDocumentStatus.Archived
        || IsPastExpiry(utcNow);

    public bool CanTransitionTo(ControlledDocumentStatus target) =>
        (Status, target) switch
        {
            (ControlledDocumentStatus.Draft, ControlledDocumentStatus.UnderReview) => true,
            (ControlledDocumentStatus.UnderReview, ControlledDocumentStatus.Approved) => true,
            (ControlledDocumentStatus.UnderReview, ControlledDocumentStatus.Draft) => true,
            (ControlledDocumentStatus.Approved, ControlledDocumentStatus.Published) => true,
            (ControlledDocumentStatus.Published, ControlledDocumentStatus.Effective) => true,
            (ControlledDocumentStatus.Effective, ControlledDocumentStatus.ReviewDue) => true,
            (ControlledDocumentStatus.ReviewDue, ControlledDocumentStatus.UnderReview) => true,
            // Reviewed with no content change — remain effective on a new review cycle.
            (ControlledDocumentStatus.ReviewDue, ControlledDocumentStatus.Effective) => true,
            (ControlledDocumentStatus.Effective, ControlledDocumentStatus.Superseded) => true,
            (ControlledDocumentStatus.ReviewDue, ControlledDocumentStatus.Superseded) => true,
            (ControlledDocumentStatus.Superseded, ControlledDocumentStatus.Archived) => true,
            (_, ControlledDocumentStatus.Archived) when Status != ControlledDocumentStatus.Archived => true,
            _ => false,
        };

    public void TransitionTo(ControlledDocumentStatus target, Guid? userId = null, DateTime? utcNow = null)
    {
        if (!CanTransitionTo(target))
            throw new InvalidOperationException($"Cannot transition from {Status} to {target}.");

        // Expired policies cannot stay (or become) Effective.
        if (target == ControlledDocumentStatus.Effective && IsPastExpiry(utcNow))
            throw new InvalidOperationException("Expired document cannot be Effective. Archive or renew before activation.");

        if (IsImmutable(utcNow) && target != ControlledDocumentStatus.Archived)
            throw new InvalidOperationException("Expired or archived documents are immutable.");

        Status = target;
        LastUpdatedAt = (utcNow ?? DateTime.UtcNow);
        LastUpdatedByUserId = userId;

        if (target == ControlledDocumentStatus.Archived)
            IsArchived = true;
    }

    public void EnsureMutable(DateTime? utcNow = null)
    {
        if (IsImmutable(utcNow))
            throw new InvalidOperationException("Expired or archived documents are immutable.");
    }
}
