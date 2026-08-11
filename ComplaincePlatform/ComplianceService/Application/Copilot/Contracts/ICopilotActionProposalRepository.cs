using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Contracts;

/// <summary>
/// Persistence contract for <see cref="CopilotActionProposal"/> and <see cref="ProposalAuditEvent"/>.
/// </summary>
public interface ICopilotActionProposalRepository
{
    Task CreateAsync(CopilotActionProposal proposal, CancellationToken ct = default);

    Task<CopilotActionProposal?> GetAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<CopilotActionProposal>> SearchAsync(
        Guid organizationId,
        ProposalStatus? status = null,
        ProposalType? proposalType = null,
        Guid? recommendationId = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    Task<IReadOnlyList<CopilotActionProposal>> GetPendingAsync(
        Guid organizationId,
        CancellationToken ct = default);

    Task UpdateAsync(CopilotActionProposal proposal, CancellationToken ct = default);

    /// <summary>Expires all proposals past their ExpiresAtUtc for a given organization.</summary>
    Task ExpireStaleAsync(Guid organizationId, CancellationToken ct = default);

    // ── Audit trail ───────────────────────────────────────────────────────────

    Task RecordAuditEventAsync(ProposalAuditEvent auditEvent, CancellationToken ct = default);

    Task<IReadOnlyList<ProposalAuditEvent>> GetAuditTrailAsync(
        Guid proposalId,
        CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
