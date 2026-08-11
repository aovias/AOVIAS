using Shared.Infrastructure.Domain;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Domain.Policies;

/// <summary>
/// Default progression: operational mode advances via <see cref="ComplianceJourney.SetOperationalMode"/> only;
/// mode changes blocked when journey is locked after submission unless in renewal window logic (future).
/// </summary>
public sealed class OperationalModeProgressionPolicy : IOperationalModeProgressionPolicy
{
    public bool CanChangeOperationalMode(global::ComplianceService.Domain.ComplianceJourney journey, out string? denyReason)
    {
        denyReason = null;
        if (journey.IsLocked && journey.Status >= ComplianceLifecycleState.Submitted)
        {
            denyReason = "Journey is locked; operational mode cannot be changed.";
            return false;
        }

        return true;
    }

    public bool IsWorkspaceReadOnly(global::ComplianceService.Domain.ComplianceJourney journey, OperationalMode mode)
    {
        _ = mode;
        if (journey.IsLocked)
        {
            return true;
        }

        return journey.Status >= ComplianceLifecycleState.Submitted;
    }

    public bool IsSnapshotReadOnly(global::ComplianceService.Domain.ComplianceJourney journey)
    {
        return journey.Status >= ComplianceLifecycleState.Approved && journey.IsLocked;
    }
}
