using ComplianceService.Domain;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.Services;

/// <summary>
/// Sprint 7A.1 — promotion readiness visibility (no promotion execution).
/// </summary>
public static class FindingPromotionReadiness
{
    public const string NotEligible = "Not Eligible";
    public const string AwaitingAcceptance = "Awaiting Acceptance";
    public const string Eligible = "Eligible";
    public const string Promoted = "Promoted";

    public static string Compute(Finding finding)
    {
        if (finding.PromotedDeficiencyId.HasValue)
        {
            return Promoted;
        }

        var classification = finding.Classification
            ?? FindingsWorkspaceQueryService.MapLegacyType(finding.Type);

        if (!FindingPromotionEligibilityService.IsClassificationEligible(classification))
        {
            return NotEligible;
        }

        if (finding.Status != FindingStatus.Accepted)
        {
            return AwaitingAcceptance;
        }

        if (!finding.ObjectiveElementId.HasValue || finding.ObjectiveElementId == Guid.Empty
            || string.IsNullOrWhiteSpace(finding.Title))
        {
            return NotEligible;
        }

        return Eligible;
    }
}
