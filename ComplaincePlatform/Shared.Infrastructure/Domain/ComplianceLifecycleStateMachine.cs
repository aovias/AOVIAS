using Shared.Infrastructure.Domain.Enums;

namespace Shared.Infrastructure.Domain;

/// <summary>
/// State machine for Core Compliance Lifecycle.
/// Defines valid state transitions and business rules for compliance journey.
/// </summary>
public static class ComplianceLifecycleStateMachine
{
    /// <summary>
    /// Determines if a state transition is valid.
    /// </summary>
    /// <param name="fromState">Current state</param>
    /// <param name="toState">Target state</param>
    /// <returns>True if transition is valid, false otherwise</returns>
    public static bool IsValidTransition(ComplianceLifecycleState fromState, ComplianceLifecycleState toState)
    {
        // Same state is always valid (no-op)
        if (fromState == toState)
        {
            return true;
        }

        return fromState switch
        {
            // Draft can transition to Enrolled
            ComplianceLifecycleState.Draft => toState == ComplianceLifecycleState.Enrolled,

            // Enrolled can transition to GapAssessment or Suspended
            ComplianceLifecycleState.Enrolled => toState == ComplianceLifecycleState.GapAssessment 
                || toState == ComplianceLifecycleState.Suspended,

            // GapAssessment can transition to InProgress, Rejected, or Suspended
            ComplianceLifecycleState.GapAssessment => toState == ComplianceLifecycleState.InProgress 
                || toState == ComplianceLifecycleState.Rejected 
                || toState == ComplianceLifecycleState.Suspended,

            // InProgress can transition to Submitted, Rejected, or Suspended
            ComplianceLifecycleState.InProgress => toState == ComplianceLifecycleState.Submitted 
                || toState == ComplianceLifecycleState.Rejected 
                || toState == ComplianceLifecycleState.Suspended,

            // Submitted can transition to AuditScheduled, Rejected, or Suspended
            ComplianceLifecycleState.Submitted => toState == ComplianceLifecycleState.AuditScheduled 
                || toState == ComplianceLifecycleState.Rejected 
                || toState == ComplianceLifecycleState.Suspended,

            // AuditScheduled can transition to AuditCompleted or Suspended
            ComplianceLifecycleState.AuditScheduled => toState == ComplianceLifecycleState.AuditCompleted 
                || toState == ComplianceLifecycleState.Suspended,

            // AuditCompleted can transition to Approved, Rejected, or Suspended
            ComplianceLifecycleState.AuditCompleted => toState == ComplianceLifecycleState.Approved 
                || toState == ComplianceLifecycleState.Rejected 
                || toState == ComplianceLifecycleState.Suspended,

            // Approved can transition to Expired, RenewalInProgress, or Suspended
            ComplianceLifecycleState.Approved => toState == ComplianceLifecycleState.Expired 
                || toState == ComplianceLifecycleState.RenewalInProgress 
                || toState == ComplianceLifecycleState.Suspended,

            // Rejected can transition back to InProgress (after fixes) or Suspended
            ComplianceLifecycleState.Rejected => toState == ComplianceLifecycleState.InProgress 
                || toState == ComplianceLifecycleState.Suspended,

            // Expired can transition to RenewalInProgress or Suspended
            ComplianceLifecycleState.Expired => toState == ComplianceLifecycleState.RenewalInProgress 
                || toState == ComplianceLifecycleState.Suspended,

            // RenewalInProgress can transition to Renewed, Rejected, or Suspended
            ComplianceLifecycleState.RenewalInProgress => toState == ComplianceLifecycleState.Renewed 
                || toState == ComplianceLifecycleState.Rejected 
                || toState == ComplianceLifecycleState.Suspended,

            // Renewed can transition to Approved (renewal approved) or Suspended
            ComplianceLifecycleState.Renewed => toState == ComplianceLifecycleState.Approved 
                || toState == ComplianceLifecycleState.Suspended,

            // Suspended (conditional) can transition to any state to resume the journey
            // The target state should be determined by framework-specific business rules
            // Typically resumes to the state before suspension or a valid next state
            ComplianceLifecycleState.Suspended => true, // Suspended can transition to any state (resume logic is framework-specific)

            _ => false
        };
    }

    /// <summary>
    /// Gets all valid next states from the current state.
    /// </summary>
    /// <param name="currentState">Current state</param>
    /// <returns>List of valid next states</returns>
    public static List<ComplianceLifecycleState> GetValidNextStates(ComplianceLifecycleState currentState)
    {
        var allStates = Enum.GetValues<ComplianceLifecycleState>();
        return allStates
            .Where(state => IsValidTransition(currentState, state))
            .ToList();
    }

    /// <summary>
    /// Checks if a state is a terminal state (no further progression possible without external action).
    /// </summary>
    /// <param name="state">State to check</param>
    /// <returns>True if terminal, false otherwise</returns>
    public static bool IsTerminalState(ComplianceLifecycleState state)
    {
        // Suspended is conditional and can be resumed, so not truly terminal
        // Expired can transition to RenewalInProgress
        // Rejected can transition back to InProgress
        // Renewed transitions to Approved
        // Approved can transition to Expired or RenewalInProgress
        return false; // No truly terminal states - all can transition with appropriate actions
    }

    /// <summary>
    /// Checks if a state represents an active compliance journey (not suspended, expired, or rejected).
    /// </summary>
    /// <param name="state">State to check</param>
    /// <returns>True if active, false otherwise</returns>
    public static bool IsActiveState(ComplianceLifecycleState state)
    {
        return state != ComplianceLifecycleState.Suspended 
            && state != ComplianceLifecycleState.Expired 
            && state != ComplianceLifecycleState.Rejected
            && state != ComplianceLifecycleState.Draft;
    }

    /// <summary>
    /// Checks if a state represents a completed/compliant state.
    /// </summary>
    /// <param name="state">State to check</param>
    /// <returns>True if compliant, false otherwise</returns>
    public static bool IsCompliantState(ComplianceLifecycleState state)
    {
        return state == ComplianceLifecycleState.Approved 
            || state == ComplianceLifecycleState.Renewed;
    }
}
