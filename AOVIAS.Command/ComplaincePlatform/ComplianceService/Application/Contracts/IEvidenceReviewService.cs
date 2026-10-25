using ComplianceService.Application.DTOs;
using ComplianceService.Application.EvidenceReview;

namespace ComplianceService.Application.Contracts;

/// <summary>
/// Evidence verification lifecycle — review decisions, queue, and audit trail.
/// Verification is a stage of <see cref="Domain.ObjectiveElementEvidence"/>, not a separate aggregate.
/// </summary>
public interface IEvidenceReviewService
{
    Task<EvidenceApprovalResultDto> ApplyReviewAsync(
        Guid evidenceId,
        Guid reviewerId,
        EvidenceReviewDecision decision,
        string? comment,
        string? recommendation,
        string? reviewerRole,
        string? expectedRowVersionBase64 = null,
        Guid? objectiveElementId = null,
        CancellationToken cancellationToken = default);

    Task<PagedResultDto<EvidenceReviewQueueItemDto>> GetReviewQueueAsync(
        Guid hospitalId,
        Guid organizationId,
        Guid frameworkId,
        EvidenceReviewQueueQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EvidenceReviewAuditEntryDto>> GetAuditTrailAsync(
        Guid hospitalId,
        Guid frameworkId,
        Guid evidenceId,
        Guid? objectiveElementId = null,
        CancellationToken cancellationToken = default);
}

public sealed class EvidenceReviewQueueQuery
{
    public string? Status { get; init; }
    public string? Stage { get; init; }
    public Guid? DepartmentId { get; init; }
    public Guid? OwnerId { get; init; }
    public string? EvidenceType { get; init; }
    public string? Search { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
