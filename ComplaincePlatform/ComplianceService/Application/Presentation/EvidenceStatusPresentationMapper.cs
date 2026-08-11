using ComplianceService.Application.EvidenceReview;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;

namespace ComplianceService.Application.Presentation;

/// <summary>
/// Maps <see cref="EvidenceVerificationStatus"/> and obligation verification enums to API keys and display labels.
/// Single presentation layer over <see cref="EvidenceVerificationStateMachine"/> — avoids duplicate client labels.
/// </summary>
public static class EvidenceStatusPresentationMapper
{
    public static string ToApiKey(EvidenceVerificationStatus status) =>
        EvidenceVerificationStateMachine.ToApiLabel(status);

    public static string ToDisplayLabel(EvidenceVerificationStatus status, EvidenceStatusPresentationContext context = EvidenceStatusPresentationContext.EvidenceItem) =>
        ToDisplayLabel(ToApiKey(status), context);

    public static string ToDisplayLabel(string? apiKey, EvidenceStatusPresentationContext context = EvidenceStatusPresentationContext.EvidenceItem)
    {
        var canonical = NormalizeCanonicalKey(apiKey, context);
        return canonical switch
        {
            "draft" => "Draft",
            "uploaded" => "Uploaded",
            "submitted" => "Submitted",
            "pendingReview" => "Pending Review",
            "approved" => context == EvidenceStatusPresentationContext.Obligation ? "Verified" : "Approved",
            "verified" => "Verified",
            "rejected" => "Rejected",
            "needsMoreEvidence" => "Needs More Evidence",
            _ => "Pending Review"
        };
    }

    public static string ObligationVerificationLabel(VerificationStatus status) =>
        status switch
        {
            VerificationStatus.Verified => "Verified",
            VerificationStatus.Rejected => "Rejected",
            VerificationStatus.Pending => "Pending Review",
            _ => "Pending Review"
        };

    public static string ObligationEvidenceLabel(EvidenceStatus status) =>
        status switch
        {
            EvidenceStatus.Verified => "Verified",
            EvidenceStatus.Submitted => "Submitted",
            EvidenceStatus.Partial => "Uploaded",
            _ => "Draft"
        };

    private static string NormalizeCanonicalKey(string? raw, EvidenceStatusPresentationContext context)
    {
        var compact = string.Concat((raw ?? string.Empty).Trim().ToLowerInvariant().Where(c => !char.IsWhiteSpace(c) && c is not '_' and not '-'));
        if (string.IsNullOrEmpty(compact))
        {
            return "pendingReview";
        }

        return compact switch
        {
            "draft" => "draft",
            "uploaded" => "uploaded",
            "submitted" => "submitted",
            "pendingreview" or "pending" or "awaitingreview" or "inreview" => "pendingReview",
            "approved" or "accepted" or "reviewed" or "complete" or "completed" => context == EvidenceStatusPresentationContext.Obligation ? "verified" : "approved",
            "verified" => context == EvidenceStatusPresentationContext.Obligation ? "verified" : "approved",
            "rejected" or "declined" => "rejected",
            "needsmoreevidence" or "needsmore" or "needsmoreinfo" or "returnforrevision" or "revisionrequested" => "needsMoreEvidence",
            "notverified" => "pendingReview",
            "missing" => "draft",
            _ => "pendingReview"
        };
    }
}

public enum EvidenceStatusPresentationContext
{
    EvidenceItem = 0,
    Obligation = 1
}
