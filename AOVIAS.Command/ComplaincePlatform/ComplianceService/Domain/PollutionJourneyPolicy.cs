using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Domain;

/// <summary>
/// Placeholder Pollution/Biomedical Waste-specific journey policy.
/// Currently delegates to core rules.
/// </summary>
public class PollutionJourneyPolicy : IFrameworkJourneyPolicy
{
    public string FrameworkCode => "POLLUTION";

    public bool CanTransition(
        ComplianceLifecycleState from,
        ComplianceLifecycleState to,
        string role,
        ComplianceJourney journey,
        out string? reason)
    {
        // TODO: Implement Pollution-specific rules (e.g., ShowCauseNotice).
        reason = null;
        return true;
    }

    public void OnTransition(
        ComplianceLifecycleState from,
        ComplianceLifecycleState to,
        ComplianceJourney journey)
    {
        // TODO: Add Pollution-specific side effects.
    }
}

