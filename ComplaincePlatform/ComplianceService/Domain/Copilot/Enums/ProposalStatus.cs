namespace ComplianceService.Domain.Copilot.Enums;

/// <summary>
/// Lifecycle states for a <see cref="CopilotActionProposal"/>.
/// Only human users may advance from Proposed → Approved or Proposed → Rejected.
/// AI systems may never approve or execute.
/// </summary>
public enum ProposalStatus
{
    /// <summary>Created by the AI and awaiting human review.</summary>
    Proposed = 0,

    /// <summary>Approved by an authorized human reviewer. Eligible for execution.</summary>
    Approved = 1,

    /// <summary>Rejected by a human reviewer. Cannot be executed.</summary>
    Rejected = 2,

    /// <summary>TTL elapsed without human action. Cannot be executed.</summary>
    Expired = 3,

    /// <summary>Approved and the underlying workflow has been successfully triggered.</summary>
    Executed = 4,
}
