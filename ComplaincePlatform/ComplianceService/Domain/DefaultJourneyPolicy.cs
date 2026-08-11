using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Domain;

/// <summary>
/// Default framework journey policy. Allows all transitions that pass the
/// core state machine and ComplianceJourneyService validations.
/// </summary>
public class DefaultJourneyPolicy : IFrameworkJourneyPolicy
{
    public string FrameworkCode => "DEFAULT";

    public bool CanTransition(
        ComplianceLifecycleState from,
        ComplianceLifecycleState to,
        string role,
        ComplianceJourney journey,
        out string? reason)
    {
        // Placeholder: no extra constraints beyond core rules.
        reason = null;
        return true;
    }

    public void OnTransition(
        ComplianceLifecycleState from,
        ComplianceLifecycleState to,
        ComplianceJourney journey)
    {
        // Placeholder: no-op for default policy.
    }
}

