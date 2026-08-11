using ComplianceService.Application.Copilot.Contracts;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Infrastructure.Copilot.Repositories;

public sealed class CopilotAnalyticsRepository : ICopilotAnalyticsRepository
{
    private readonly ComplianceDbContext _db;

    public CopilotAnalyticsRepository(ComplianceDbContext db) => _db = db;

    public async Task<RecommendationCounts> GetRecommendationCountsAsync(
        Guid organizationId,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        CancellationToken ct = default)
    {
        var q = _db.CopilotRecommendations.Where(r => r.OrganizationId == organizationId);
        if (fromUtc.HasValue) q = q.Where(r => r.GeneratedAtUtc >= fromUtc.Value);
        if (toUtc.HasValue) q = q.Where(r => r.GeneratedAtUtc < toUtc.Value);

        var items = await q.GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return new RecommendationCounts
        {
            Total     = items.Sum(x => x.Count),
            Proposed  = items.FirstOrDefault(x => x.Status == RecommendationStatus.Proposed)?.Count ?? 0,
            Accepted  = items.FirstOrDefault(x => x.Status == RecommendationStatus.Accepted)?.Count ?? 0,
            Dismissed = items.FirstOrDefault(x => x.Status == RecommendationStatus.Dismissed)?.Count ?? 0,
            Expired   = items.FirstOrDefault(x => x.Status == RecommendationStatus.Expired)?.Count ?? 0,
        };
    }

    public async Task<DraftCounts> GetDraftCountsAsync(
        Guid organizationId,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        CancellationToken ct = default)
    {
        var q = _db.AiDrafts.Where(d => d.OrganizationId == organizationId);
        if (fromUtc.HasValue) q = q.Where(d => d.GeneratedAtUtc >= fromUtc.Value);
        if (toUtc.HasValue) q = q.Where(d => d.GeneratedAtUtc < toUtc.Value);

        var total = await q.CountAsync(ct);
        var archived = await q.CountAsync(d => d.Status == DraftStatus.Archived, ct);

        var regeneratedGroups = await q
            .GroupBy(d => d.GroupId)
            .Where(g => g.Max(d => d.Version) > 1)
            .CountAsync(ct);

        return new DraftCounts
        {
            Total              = total,
            Active             = total - archived,
            Archived           = archived,
            RegeneratedGroups  = regeneratedGroups,
        };
    }

    public async Task<ProposalCounts> GetProposalCountsAsync(
        Guid organizationId,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        CancellationToken ct = default)
    {
        var q = _db.CopilotActionProposals.Where(p => p.OrganizationId == organizationId);
        if (fromUtc.HasValue) q = q.Where(p => p.ProposedAtUtc >= fromUtc.Value);
        if (toUtc.HasValue) q = q.Where(p => p.ProposedAtUtc < toUtc.Value);

        var items = await q.GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return new ProposalCounts
        {
            Total    = items.Sum(x => x.Count),
            Proposed = items.FirstOrDefault(x => x.Status == ProposalStatus.Proposed)?.Count ?? 0,
            Approved = items.FirstOrDefault(x => x.Status == ProposalStatus.Approved)?.Count ?? 0,
            Rejected = items.FirstOrDefault(x => x.Status == ProposalStatus.Rejected)?.Count ?? 0,
            Executed = items.FirstOrDefault(x => x.Status == ProposalStatus.Executed)?.Count ?? 0,
            Expired  = items.FirstOrDefault(x => x.Status == ProposalStatus.Expired)?.Count ?? 0,
        };
    }

    public async Task<IReadOnlyList<LabelCount>> GetRecommendationCountsByCategoryAsync(
        Guid organizationId,
        CancellationToken ct = default) =>
        await _db.CopilotRecommendations
            .Where(r => r.OrganizationId == organizationId)
            .GroupBy(r => r.Category)
            .Select(g => new LabelCount { Label = g.Key.ToString(), Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<LabelCount>> GetRecommendationCountsByPersonaAsync(
        Guid organizationId,
        CancellationToken ct = default) =>
        await _db.CopilotRecommendations
            .Where(r => r.OrganizationId == organizationId)
            .GroupBy(r => r.Persona)
            .Select(g => new LabelCount { Label = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<LabelCount>> GetDraftCountsByTypeAsync(
        Guid organizationId,
        CancellationToken ct = default) =>
        await _db.AiDrafts
            .Where(d => d.OrganizationId == organizationId)
            .GroupBy(d => d.DraftType)
            .Select(g => new LabelCount { Label = g.Key.ToString(), Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<LabelCount>> GetProposalCountsByTypeAsync(
        Guid organizationId,
        CancellationToken ct = default) =>
        await _db.CopilotActionProposals
            .Where(p => p.OrganizationId == organizationId)
            .GroupBy(p => p.ProposalType)
            .Select(g => new LabelCount { Label = g.Key.ToString(), Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync(ct);

    public async Task<int> CountRecommendationsWithProposalsAsync(Guid organizationId, CancellationToken ct = default) =>
        await _db.CopilotActionProposals
            .Where(p => p.OrganizationId == organizationId)
            .Select(p => p.RecommendationId)
            .Distinct()
            .CountAsync(ct);

    public async Task<int> CountRecommendationsWithExecutedProposalsAsync(Guid organizationId, CancellationToken ct = default) =>
        await _db.CopilotActionProposals
            .Where(p => p.OrganizationId == organizationId && p.Status == ProposalStatus.Executed)
            .Select(p => p.RecommendationId)
            .Distinct()
            .CountAsync(ct);

    public async Task<int> CountDraftsWithProposalsAsync(Guid organizationId, CancellationToken ct = default) =>
        await _db.CopilotActionProposals
            .Where(p => p.OrganizationId == organizationId && p.DraftId != null)
            .Select(p => p.DraftId!)
            .Distinct()
            .CountAsync(ct);

    public async Task<double?> GetAverageRecommendationTimeToActionHoursAsync(
        Guid organizationId,
        CancellationToken ct = default)
    {
        var acted = await _db.CopilotRecommendations
            .Where(r => r.OrganizationId == organizationId &&
                        (r.AcceptedAtUtc != null || r.DismissedAtUtc != null))
            .Select(r => new
            {
                Generated = r.GeneratedAtUtc,
                Actioned  = r.AcceptedAtUtc ?? r.DismissedAtUtc,
            })
            .ToListAsync(ct);

        if (acted.Count == 0) return null;
        return acted.Average(r => (r.Actioned!.Value - r.Generated).TotalHours);
    }

    public async Task<double?> GetAverageProposalApprovalTimeHoursAsync(
        Guid organizationId,
        CancellationToken ct = default)
    {
        var approved = await _db.CopilotActionProposals
            .Where(p => p.OrganizationId == organizationId && p.ApprovedAtUtc != null)
            .Select(p => new { p.ProposedAtUtc, p.ApprovedAtUtc })
            .ToListAsync(ct);

        if (approved.Count == 0) return null;
        return approved.Average(p => (p.ApprovedAtUtc!.Value - p.ProposedAtUtc).TotalHours);
    }

    public async Task<double?> GetAverageProposalExecutionTimeHoursAsync(
        Guid organizationId,
        CancellationToken ct = default)
    {
        var executed = await _db.CopilotActionProposals
            .Where(p => p.OrganizationId == organizationId && p.ExecutedAtUtc != null && p.ApprovedAtUtc != null)
            .Select(p => new { p.ApprovedAtUtc, p.ExecutedAtUtc })
            .ToListAsync(ct);

        if (executed.Count == 0) return null;
        return executed.Average(p => (p.ExecutedAtUtc!.Value - p.ApprovedAtUtc!.Value).TotalHours);
    }

    public async Task<int> CountProposalAuditViewedAsync(Guid organizationId, CancellationToken ct = default) =>
        await _db.CopilotProposalAuditEvents
            .Where(e => e.OrganizationId == organizationId &&
                        e.EventType == ProposalAuditEvent.EventTypes.Viewed)
            .CountAsync(ct);

    public async Task<IReadOnlyList<RecommendationImpactRow>> GetRecommendationImpactRowsAsync(
        Guid organizationId,
        int limit = 20,
        CancellationToken ct = default)
    {
        var proposals = await _db.CopilotActionProposals
            .Where(p => p.OrganizationId == organizationId)
            .Select(p => new
            {
                p.RecommendationId,
                p.Status,
                p.ResultEntityType,
                p.ExecutionNotes,
            })
            .ToListAsync(ct);

        var proposalMap = proposals
            .GroupBy(p => p.RecommendationId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(p => p.Status == ProposalStatus.Executed).First());

        var recs = await _db.CopilotRecommendations
            .Where(r => r.OrganizationId == organizationId)
            .OrderByDescending(r => r.GeneratedAtUtc)
            .Take(limit * 2)
            .ToListAsync(ct);

        return recs
            .Where(r => proposalMap.ContainsKey(r.Id) || r.Status == RecommendationStatus.Accepted)
            .Take(limit)
            .Select(r =>
            {
                var p = proposalMap.GetValueOrDefault(r.Id);
                return new RecommendationImpactRow
                {
                    RecommendationId   = r.Id,
                    Title              = r.Title,
                    Category           = r.Category.ToString(),
                    Persona            = r.Persona,
                    ImpactJson         = r.ImpactJson,
                    HasProposal        = p != null,
                    ProposalExecuted   = p?.Status == ProposalStatus.Executed,
                    ResultEntityType   = p?.ResultEntityType,
                    ExecutionNotes     = p?.ExecutionNotes,
                };
            })
            .ToList();
    }

    public async Task SaveEffectivenessSnapshotAsync(CopilotEffectivenessSnapshot snapshot, CancellationToken ct = default)
    {
        await _db.CopilotEffectivenessSnapshots.AddAsync(snapshot, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<CopilotEffectivenessSnapshot>> GetEffectivenessSnapshotsAsync(
        Guid organizationId,
        int days = 30,
        CancellationToken ct = default)
    {
        var from = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-days));
        return await _db.CopilotEffectivenessSnapshots
            .Where(s => s.OrganizationId == organizationId && s.SnapshotDate >= from)
            .OrderBy(s => s.SnapshotDate)
            .ToListAsync(ct);
    }

    public async Task<CopilotEffectivenessSnapshot?> GetEffectivenessSnapshotForDateAsync(
        Guid organizationId,
        DateOnly date,
        CancellationToken ct = default) =>
        await _db.CopilotEffectivenessSnapshots
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId && s.SnapshotDate == date, ct);
}
