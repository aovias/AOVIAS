using ComplianceService.Application.EvidenceRegistration;

namespace ComplianceService.Application.Contracts;

/// <summary>
/// Single write path for evidence registration against compliance requirements.
/// All UI entry points (Journey, Tasks, Evidence Library, Evidence Binder) converge here.
/// </summary>
public interface IEvidenceRegistrationService
{
    /// <summary>Multipart file upload registration pipeline.</summary>
    Task<EvidenceRegistrationResult> RegisterFromUploadAsync(
        EvidenceRegistrationUploadRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>URL-reference registration (JSON register flows).</summary>
    Task<EvidenceRegistrationResult> RegisterFromUrlAsync(
        EvidenceRegistrationUrlRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Links an existing evidence row to an objective element and runs the post-registration pipeline.
    /// Used when evidence persistence precedes linkage (legacy/scoring flows).
    /// </summary>
    Task<EvidenceRegistrationResult> LinkExistingEvidenceAsync(
        EvidenceRegistrationLinkRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Read-only integrity audit: orphaned evidence, missing OE links, duplicate file hashes.
    /// </summary>
    Task<EvidenceIntegrityReport> AuditIntegrityAsync(
        Guid hospitalId,
        Guid frameworkId,
        CancellationToken cancellationToken = default);
}

public sealed class EvidenceRegistrationContext
{
    public Guid TenantId { get; init; }
    public Guid UserId { get; init; }
    public Guid? JourneyId { get; init; }
    public Guid? TaskId { get; init; }
    public Guid? ObjectiveElementId { get; init; }
    public Guid? OrganizationComplianceObligationId { get; init; }
    public Guid? ComplianceDeficiencyId { get; init; }

    public void ValidateAnchors()
    {
        var hasTask = TaskId is Guid t && t != Guid.Empty;
        var hasOe = ObjectiveElementId is Guid oe && oe != Guid.Empty;
        var hasObl = OrganizationComplianceObligationId is Guid o && o != Guid.Empty;
        var hasDef = ComplianceDeficiencyId is Guid d && d != Guid.Empty;

        if (!hasTask && !hasOe && !hasObl && !hasDef)
        {
            throw new ArgumentException(
                "Evidence registration requires at least one context anchor: TaskId, ObjectiveElementId, " +
                "OrganizationComplianceObligationId, or ComplianceDeficiencyId.");
        }
    }

    public static EvidenceRegistrationContext ForJourneyObjectiveElement(
        Guid journeyId,
        Guid objectiveElementId,
        Guid tenantId,
        Guid userId) =>
        new()
        {
            JourneyId = journeyId,
            ObjectiveElementId = objectiveElementId,
            TenantId = tenantId,
            UserId = userId
        };

    public static EvidenceRegistrationContext ForTask(
        Guid taskId,
        Guid tenantId,
        Guid userId) =>
        new()
        {
            TaskId = taskId,
            TenantId = tenantId,
            UserId = userId
        };
}

public sealed class EvidenceRegistrationOptions
{
    /// <summary>When true, snapshot recalculation is deferred (bulk upload batches enqueue once).</summary>
    public bool SuppressSnapshotRecalc { get; init; }

    /// <summary>Pre-built metadata JSON; when set, skips default metadata builder.</summary>
    public string? MetadataJsonOverride { get; init; }

    public string? Remarks { get; init; }
}

public sealed class EvidenceRegistrationUploadRequest
{
    public required EvidenceRegistrationContext Context { get; init; }
    public required Stream FileStream { get; init; }
    public required string FileName { get; init; }
    public string? ContentType { get; init; }
    public required string EvidenceType { get; init; }
    public string? DisplayName { get; init; }
    public string? Title { get; init; }
    public string? Metadata { get; init; }
    public EvidenceRegistrationOptions Options { get; init; } = new();
}

public sealed class EvidenceRegistrationUrlRequest
{
    public required EvidenceRegistrationContext Context { get; init; }
    public required string EvidenceType { get; init; }
    public required string FileUrl { get; init; }
    public string? Title { get; init; }
    public string? Metadata { get; init; }
    public EvidenceRegistrationOptions Options { get; init; } = new();
}

public sealed class EvidenceRegistrationLinkRequest
{
    public required Guid EvidenceId { get; init; }
    public required Guid ObjectiveElementId { get; init; }
    public required Guid LinkedByUserId { get; init; }
    public Guid HospitalId { get; init; }
    public Guid? JourneyId { get; init; }
    public EvidenceRegistrationOptions Options { get; init; } = new();
}

public sealed record EvidenceRegistrationResult(
    Guid EvidenceId,
    Guid TaskId,
    Guid ObjectiveElementId,
    Guid HospitalId,
    Guid FrameworkId);

public sealed class EvidenceIntegrityReport
{
    public int OrphanedEvidenceCount { get; init; }
    public int MissingObjectiveElementLinkCount { get; init; }
    public int DuplicateHashCount { get; init; }
    public IReadOnlyList<Guid> OrphanedEvidenceIds { get; init; } = Array.Empty<Guid>();
    public IReadOnlyList<Guid> EvidenceMissingLinkIds { get; init; } = Array.Empty<Guid>();
}

/// <summary>
/// Future verification workflow extension point (Phase 10). Not implemented — approval remains on <see cref="IEvidenceService.ApplyApprovalAsync"/>.
/// </summary>
public interface IEvidenceVerificationWorkflow
{
    Task OnEvidenceSubmittedForReviewAsync(Guid evidenceId, Guid objectiveElementId, CancellationToken cancellationToken = default);
}
