using ComplianceService.Application.Copilot.DTOs;

namespace ComplianceService.Application.Copilot.Contracts;

/// <summary>
/// Executes an approved <see cref="Domain.Copilot.CopilotActionProposal"/> by invoking
/// the corresponding existing application service command.
///
/// CRITICAL CONSTRAINTS:
/// - Only executes Approved proposals (validated before dispatch)
/// - Uses EXISTING services only — no new business logic
/// - Records ResultEntityId + ResultEntityType back on the proposal
/// - Never auto-approves
/// </summary>
public interface IProposalExecutionService
{
    /// <summary>
    /// Executes an approved proposal.
    /// Validates approval status, dispatches to the correct existing service,
    /// records the resulting entity ID, and writes an audit event.
    /// </summary>
    Task<ProposalExecutionResultDto> ExecuteAsync(
        Guid proposalId,
        Guid executingUserId,
        CancellationToken ct = default);
}
