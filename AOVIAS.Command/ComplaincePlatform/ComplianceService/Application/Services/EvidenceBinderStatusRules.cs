namespace ComplianceService.Application.Services;

/// <summary>
/// Strict binder status: Missing → no evidence; Partial → evidence exists but none approved;
/// Approved → at least one approved link (pending/rejected ignored when any approved exists).
/// </summary>
public static class EvidenceBinderStatusRules
{
    public const string Missing = "missing";
    public const string Partial = "partial";
    public const string Approved = "approved";

    public static string ComputeObjectiveStatus(bool hasAnyEvidenceLink, bool hasAnyApprovedLink)
    {
        if (!hasAnyEvidenceLink)
        {
            return Missing;
        }

        if (hasAnyApprovedLink)
        {
            return Approved;
        }

        return Partial;
    }

    public static string ComputeChapterRiskLevel(int missingCount, int partialCount)
    {
        if (missingCount > 0)
        {
            return "high";
        }

        if (partialCount > 0)
        {
            return "medium";
        }

        return "low";
    }
}
