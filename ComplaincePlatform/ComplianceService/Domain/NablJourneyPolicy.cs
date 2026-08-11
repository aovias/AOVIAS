using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Domain;

/// <summary>
/// Placeholder NABL-specific journey policy. Currently delegates to core rules.
/// </summary>
public class NablJourneyPolicy : IFrameworkJourneyPolicy
{
    public string FrameworkCode => "NABL";

    public bool CanTransition(
        ComplianceLifecycleState from,
        ComplianceLifecycleState to,
        string role,
        ComplianceJourney journey,
        out string? reason)
    {
        // TODO: Implement NABL-specific rules (e.g., calibration & proficiency testing).
        reason = null;
        return true;
    }

    public void OnTransition(
        ComplianceLifecycleState from,
        ComplianceLifecycleState to,
        ComplianceJourney journey)
    {
        // TODO: Add NABL-specific side effects.
    }
}

