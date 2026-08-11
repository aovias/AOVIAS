using ComplianceService.Domain;

namespace ComplianceService.Application.EvidenceReview;

/// <summary>
/// Verification lifecycle state machine for <see cref="ObjectiveElementEvidence"/>.
/// Verification is a stage of evidence — not a separate aggregate.
/// </summary>
public static class EvidenceVerificationStateMachine
{
    public static bool CanTransition(EvidenceVerificationStatus from, EvidenceVerificationStatus to) =>
        (from, to) switch
        {
            (EvidenceVerificationStatus.Draft, EvidenceVerificationStatus.Submitted) => true,
            (EvidenceVerificationStatus.Draft, EvidenceVerificationStatus.Pending) => true,
            (EvidenceVerificationStatus.Submitted, EvidenceVerificationStatus.Pending) => true,
            (EvidenceVerificationStatus.Pending, EvidenceVerificationStatus.Verified) => true,
            (EvidenceVerificationStatus.Pending, EvidenceVerificationStatus.Rejected) => true,
            (EvidenceVerificationStatus.Pending, EvidenceVerificationStatus.NeedsMoreEvidence) => true,
            (EvidenceVerificationStatus.Rejected, EvidenceVerificationStatus.Pending) => true,
            (EvidenceVerificationStatus.Rejected, EvidenceVerificationStatus.Submitted) => true,
            (EvidenceVerificationStatus.NeedsMoreEvidence, EvidenceVerificationStatus.Submitted) => true,
            (EvidenceVerificationStatus.NeedsMoreEvidence, EvidenceVerificationStatus.Pending) => true,
            // Verified is terminal (closed) — no further mutations including double-approve
            (EvidenceVerificationStatus.Verified, EvidenceVerificationStatus.Verified) => false,
            (EvidenceVerificationStatus.Verified, _) => false,
            _ => false
        };

    public static EvidenceVerificationStatus MapReviewAction(EvidenceReviewDecision decision) =>
        decision switch
        {
            EvidenceReviewDecision.Approve => EvidenceVerificationStatus.Verified,
            EvidenceReviewDecision.Reject => EvidenceVerificationStatus.Rejected,
            EvidenceReviewDecision.RequestMoreEvidence => EvidenceVerificationStatus.NeedsMoreEvidence,
            _ => throw new ArgumentOutOfRangeException(nameof(decision))
        };

    public static bool IsAwaitingReview(EvidenceVerificationStatus status) =>
        status is EvidenceVerificationStatus.Pending
            or EvidenceVerificationStatus.Submitted;

    public static bool IsApproved(EvidenceVerificationStatus status) =>
        status == EvidenceVerificationStatus.Verified;

    public static bool BlocksTaskCompletion(EvidenceVerificationStatus status) =>
        IsAwaitingReview(status)
        || status == EvidenceVerificationStatus.Rejected
        || status == EvidenceVerificationStatus.NeedsMoreEvidence;

    public static string ToApiLabel(EvidenceVerificationStatus status) =>
        status switch
        {
            EvidenceVerificationStatus.Draft => "draft",
            EvidenceVerificationStatus.Submitted => "submitted",
            EvidenceVerificationStatus.Pending => "pendingReview",
            EvidenceVerificationStatus.Verified => "approved",
            EvidenceVerificationStatus.Rejected => "rejected",
            EvidenceVerificationStatus.NeedsMoreEvidence => "needsMoreEvidence",
            _ => "unknown"
        };

    public static EvidenceVerificationStatus? ParseFilter(string? filter) =>
        string.IsNullOrWhiteSpace(filter)
            ? null
            : filter.Trim().ToLowerInvariant() switch
            {
                "pendingreview" or "pending" => EvidenceVerificationStatus.Pending,
                "submitted" => EvidenceVerificationStatus.Submitted,
                "approved" or "verified" => EvidenceVerificationStatus.Verified,
                "rejected" => EvidenceVerificationStatus.Rejected,
                "needsmoreevidence" or "needs-more" => EvidenceVerificationStatus.NeedsMoreEvidence,
                "draft" => EvidenceVerificationStatus.Draft,
                _ => null
            };
}

public enum EvidenceReviewDecision
{
    Approve = 1,
    Reject = 2,
    RequestMoreEvidence = 3
}
