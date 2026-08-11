using ComplianceService.Application.Copilot.Contracts;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Services;

/// <summary>
/// Evaluates authorization rules for proposal lifecycle transitions.
/// No AI system ever satisfies these checks — requires authenticated human user IDs.
/// All checks are synchronous; no I/O required.
/// </summary>
public sealed class ApprovalPolicyService : IApprovalPolicyService
{
    // Roles authorized to approve/reject proposals
    private static readonly HashSet<string> ApprovalRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "ComplianceManager",
        "QualityOfficer",
        "ComplianceOfficer",
        "HospitalAdmin",
        "Admin",
        "SuperAdmin",
    };

    // Roles authorized to execute approved proposals (trigger existing workflows)
    private static readonly HashSet<string> ExecutionRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "ComplianceManager",
        "QualityOfficer",
        "ComplianceOfficer",
        "HospitalAdmin",
        "Admin",
        "SuperAdmin",
    };

    public PolicyCheckResult CanApprove(
        CopilotActionProposal proposal,
        Guid userId,
        IReadOnlyList<string> userRoles)
    {
        if (userId == Guid.Empty)
            return PolicyCheckResult.Deny("User is not authenticated.");

        if (proposal.Status != ProposalStatus.Proposed)
            return PolicyCheckResult.Deny($"Proposal is not in Proposed status (current: {proposal.Status}).");

        if (DateTime.UtcNow > proposal.ExpiresAtUtc)
            return PolicyCheckResult.Deny("Proposal has expired.");

        if (!userRoles.Any(r => ApprovalRoles.Contains(r)))
            return PolicyCheckResult.Deny("User does not have an approval-authorized role.");

        return PolicyCheckResult.Allow();
    }

    public PolicyCheckResult CanReject(
        CopilotActionProposal proposal,
        Guid userId,
        IReadOnlyList<string> userRoles)
    {
        if (userId == Guid.Empty)
            return PolicyCheckResult.Deny("User is not authenticated.");

        if (proposal.Status != ProposalStatus.Proposed)
            return PolicyCheckResult.Deny($"Only Proposed proposals can be rejected (current: {proposal.Status}).");

        if (DateTime.UtcNow > proposal.ExpiresAtUtc)
            return PolicyCheckResult.Deny("Proposal has expired.");

        if (!userRoles.Any(r => ApprovalRoles.Contains(r)))
            return PolicyCheckResult.Deny("User does not have an approval-authorized role.");

        return PolicyCheckResult.Allow();
    }

    public PolicyCheckResult CanExecute(
        CopilotActionProposal proposal,
        Guid userId,
        IReadOnlyList<string> userRoles)
    {
        if (userId == Guid.Empty)
            return PolicyCheckResult.Deny("User is not authenticated.");

        if (proposal.Status != ProposalStatus.Approved)
            return PolicyCheckResult.Deny($"Only Approved proposals can be executed (current: {proposal.Status}).");

        if (!userRoles.Any(r => ExecutionRoles.Contains(r)))
            return PolicyCheckResult.Deny("User does not have an execution-authorized role.");

        return PolicyCheckResult.Allow();
    }
}
