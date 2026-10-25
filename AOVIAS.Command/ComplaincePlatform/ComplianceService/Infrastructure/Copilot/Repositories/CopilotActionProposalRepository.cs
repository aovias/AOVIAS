using ComplianceService.Application.Copilot.Contracts;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Infrastructure.Copilot.Repositories;

public sealed class CopilotActionProposalRepository : ICopilotActionProposalRepository
{
    private readonly ComplianceDbContext _db;

    public CopilotActionProposalRepository(ComplianceDbContext db)
    {
        _db = db;
    }

    // ── CRUD ─────────────────────────────────────────────────────────────────

    public async Task CreateAsync(CopilotActionProposal proposal, CancellationToken ct = default)
    {
        await _db.CopilotActionProposals.AddAsync(proposal, ct);
    }

    public async Task<CopilotActionProposal?> GetAsync(Guid id, CancellationToken ct = default) =>
        await _db.CopilotActionProposals.FindAsync(new object[] { id }, ct);

    public async Task<IReadOnlyList<CopilotActionProposal>> SearchAsync(
        Guid organizationId,
        ProposalStatus? status = null,
        ProposalType? proposalType = null,
        Guid? recommendationId = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = _db.CopilotActionProposals
            .Where(p => p.OrganizationId == organizationId);

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        if (proposalType.HasValue)
            query = query.Where(p => p.ProposalType == proposalType.Value);

        if (recommendationId.HasValue)
            query = query.Where(p => p.RecommendationId == recommendationId.Value);

        return await query
            .OrderByDescending(p => p.ProposedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<CopilotActionProposal>> GetPendingAsync(
        Guid organizationId,
        CancellationToken ct = default) =>
        await _db.CopilotActionProposals
            .Where(p => p.OrganizationId == organizationId
                     && p.Status == ProposalStatus.Proposed
                     && p.ExpiresAtUtc > DateTime.UtcNow)
            .OrderByDescending(p => p.ProposedAtUtc)
            .ToListAsync(ct);

    public async Task UpdateAsync(CopilotActionProposal proposal, CancellationToken ct = default)
    {
        _db.CopilotActionProposals.Update(proposal);
        await Task.CompletedTask;
    }

    public async Task ExpireStaleAsync(Guid organizationId, CancellationToken ct = default)
    {
        var stale = await _db.CopilotActionProposals
            .Where(p => p.OrganizationId == organizationId
                     && p.Status == ProposalStatus.Proposed
                     && p.ExpiresAtUtc <= DateTime.UtcNow)
            .ToListAsync(ct);

        foreach (var proposal in stale)
            proposal.Expire();
    }

    // ── Audit trail ───────────────────────────────────────────────────────────

    public async Task RecordAuditEventAsync(ProposalAuditEvent auditEvent, CancellationToken ct = default)
    {
        await _db.CopilotProposalAuditEvents.AddAsync(auditEvent, ct);
    }

    public async Task<IReadOnlyList<ProposalAuditEvent>> GetAuditTrailAsync(
        Guid proposalId,
        CancellationToken ct = default) =>
        await _db.CopilotProposalAuditEvents
            .Where(e => e.ProposalId == proposalId)
            .OrderBy(e => e.OccurredAtUtc)
            .ToListAsync(ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }
}
