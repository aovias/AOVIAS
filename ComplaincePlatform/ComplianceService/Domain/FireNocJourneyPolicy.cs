using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Domain;

/// <summary>
/// Placeholder Fire NOC-specific journey policy. Currently delegates to core rules.
/// </summary>
public class FireNocJourneyPolicy : IFrameworkJourneyPolicy
{
    public string FrameworkCode => "FIRE_NOC";

    public bool CanTransition(
        ComplianceLifecycleState from,
        ComplianceLifecycleState to,
        string role,
        ComplianceJourney journey,
        out string? reason)
    {
        // TODO: Implement Fire NOC-specific rules (e.g., ConditionalApproval).
        reason = null;
        return true;
    }

    public void OnTransition(
        ComplianceLifecycleState from,
        ComplianceLifecycleState to,
        ComplianceJourney journey)
    {
        // TODO: Add Fire NOC-specific side effects.
    }
}

