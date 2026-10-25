using ComplianceService.Domain.Enums;

namespace ComplianceService.Domain;

/// <summary>Sprint 8 — deficiency lifecycle state machine (maps to persisted <see cref="DeficiencyStatus"/>).</summary>
public static class DeficiencyLifecycleTransitions
{
    public static DeficiencyLifecycleStatus ToLifecycle(DeficiencyStatus status) =>
        status switch
        {
            DeficiencyStatus.Identified => DeficiencyLifecycleStatus.Identified,
            DeficiencyStatus.Assigned => DeficiencyLifecycleStatus.Assigned,
            DeficiencyStatus.InProgress => DeficiencyLifecycleStatus.InProgress,
            DeficiencyStatus.EvidenceSubmitted => DeficiencyLifecycleStatus.AwaitingEvidence,
            DeficiencyStatus.ReviewPending => DeficiencyLifecycleStatus.VerificationPending,
            DeficiencyStatus.Verified => DeficiencyLifecycleStatus.Verified,
            DeficiencyStatus.Closed => DeficiencyLifecycleStatus.Closed,
            DeficiencyStatus.Cancelled => DeficiencyLifecycleStatus.Cancelled,
            _ => DeficiencyLifecycleStatus.Identified,
        };

    public static DeficiencyStatus FromLifecycle(DeficiencyLifecycleStatus lifecycle) =>
        lifecycle switch
        {
            DeficiencyLifecycleStatus.Identified => DeficiencyStatus.Identified,
            DeficiencyLifecycleStatus.Assigned => DeficiencyStatus.Assigned,
            DeficiencyLifecycleStatus.InProgress => DeficiencyStatus.InProgress,
            DeficiencyLifecycleStatus.AwaitingEvidence => DeficiencyStatus.EvidenceSubmitted,
            DeficiencyLifecycleStatus.VerificationPending => DeficiencyStatus.ReviewPending,
            DeficiencyLifecycleStatus.Verified => DeficiencyStatus.Verified,
            DeficiencyLifecycleStatus.Closed => DeficiencyStatus.Closed,
            DeficiencyLifecycleStatus.Cancelled => DeficiencyStatus.Cancelled,
            _ => DeficiencyStatus.Identified,
        };

    public static string GetDisplayName(DeficiencyLifecycleStatus lifecycle) =>
        lifecycle switch
        {
            DeficiencyLifecycleStatus.InProgress => "In Progress",
            DeficiencyLifecycleStatus.AwaitingEvidence => "Awaiting Evidence",
            DeficiencyLifecycleStatus.VerificationPending => "Verification Pending",
            _ => lifecycle.ToString(),
        };

    public static bool CanTransition(DeficiencyStatus current, DeficiencyStatus target)
    {
        if (current == DeficiencyStatus.Closed || current == DeficiencyStatus.Cancelled)
        {
            return false;
        }

        if (target == DeficiencyStatus.Closed || target == DeficiencyStatus.Cancelled)
        {
            return true;
        }

        if (target == DeficiencyStatus.InProgress)
        {
            return current is DeficiencyStatus.Assigned
                or DeficiencyStatus.InProgress
                or DeficiencyStatus.EvidenceSubmitted
                or DeficiencyStatus.ReviewPending;
        }

        return (int)target >= (int)current;
    }

    public static DeficiencyRiskLevel ComputeRiskLevel(int likelihood, int impact)
    {
        var score = likelihood * impact;
        return score switch
        {
            >= 20 => DeficiencyRiskLevel.Critical,
            >= 12 => DeficiencyRiskLevel.High,
            >= 6 => DeficiencyRiskLevel.Medium,
            _ => DeficiencyRiskLevel.Low,
        };
    }
}
