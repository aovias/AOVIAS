using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Domain;

/// <summary>
/// Framework-specific policy for compliance journey lifecycle transitions.
/// Provides hooks for additional validation and side effects around the core
/// ComplianceLifecycleState machine.
/// </summary>
public interface IFrameworkJourneyPolicy
{
    /// <summary>
    /// Framework code identifier, e.g., "NABH", "NABL", "FIRE_NOC", "POLLUTION".
    /// </summary>
    string FrameworkCode { get; }

    /// <summary>
    /// Determines whether a transition is allowed for this framework, given
    /// current and target states, caller role, and journey context.
    /// </summary>
    bool CanTransition(
        ComplianceLifecycleState from,
        ComplianceLifecycleState to,
        string role,
        ComplianceJourney journey,
        out string? reason);

    /// <summary>
    /// Hook invoked after a successful transition. Intended for framework-
    /// specific side effects such as scheduling surveillance audits, etc.
    /// </summary>
    void OnTransition(
        ComplianceLifecycleState from,
        ComplianceLifecycleState to,
        ComplianceJourney journey);
}

