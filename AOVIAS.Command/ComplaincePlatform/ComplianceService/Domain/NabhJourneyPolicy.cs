using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Domain;

/// <summary>
/// NABH framework journey policy. Accreditation scoring and OE workspace rules are enforced in
/// <c>JourneyWorkspaceAppService</c> and <c>NabhAccreditationEvaluator</c>; lifecycle transitions here
/// add only framework-specific gates beyond the core state machine when needed.
/// </summary>
public class NabhJourneyPolicy : IFrameworkJourneyPolicy
{
    public string FrameworkCode => "NABH";

    public bool CanTransition(
        ComplianceLifecycleState from,
        ComplianceLifecycleState to,
        string role,
        ComplianceJourney journey,
        out string? reason)
    {
        // No extra constraints beyond core rules and ComplianceJourneyService validations yet.
        reason = null;
        return true;
    }

    public void OnTransition(
        ComplianceLifecycleState from,
        ComplianceLifecycleState to,
        ComplianceJourney journey)
    {
        // Side effects (e.g. surveillance scheduling) are out of scope for the workspace module.
    }
}

