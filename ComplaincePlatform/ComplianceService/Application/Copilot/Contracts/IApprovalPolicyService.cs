using ComplianceService.Domain.Copilot;

namespace ComplianceService.Application.Copilot.Contracts;

/// <summary>
/// Evaluates whether a user has authority to approve, reject, or execute a proposal.
/// No AI system may ever satisfy these checks — only authenticated human users.
/// </summary>
public interface IApprovalPolicyService
{
    /// <summary>
    /// Returns true if the given user is authorized to approve the proposal.
    /// Checks: authenticated human, same organization, appropriate role, proposal is Proposed status.
    /// </summary>
    PolicyCheckResult CanApprove(CopilotActionProposal proposal, Guid userId, IReadOnlyList<string> userRoles);

    /// <summary>
    /// Returns true if the given user is authorized to reject the proposal.
    /// </summary>
    PolicyCheckResult CanReject(CopilotActionProposal proposal, Guid userId, IReadOnlyList<string> userRoles);

    /// <summary>
    /// Returns true if the given user is authorized to execute an approved proposal.
    /// Requires: proposal is Approved, user has execution authority, not already executed.
    /// </summary>
    PolicyCheckResult CanExecute(CopilotActionProposal proposal, Guid userId, IReadOnlyList<string> userRoles);
}

/// <summary>Result of a policy check with an optional denial reason.</summary>
public sealed class PolicyCheckResult
{
    public bool IsAllowed { get; init; }
    public string? DenialReason { get; init; }

    public static PolicyCheckResult Allow() => new() { IsAllowed = true };
    public static PolicyCheckResult Deny(string reason) => new() { IsAllowed = false, DenialReason = reason };
}
