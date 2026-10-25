using ComplianceService.Application.Copilot.Contracts;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;
using ComplianceService.Infrastructure.Persistence;
using ComplianceService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Infrastructure.Copilot.Repositories;

public sealed class CopilotRecommendationRepository : Repository<CopilotRecommendation>, ICopilotRecommendationRepository
{
    public CopilotRecommendationRepository(ComplianceDbContext db) : base(db) { }

    public async Task CreateAsync(CopilotRecommendation recommendation, CancellationToken cancellationToken = default)
    {
        await _db.CopilotRecommendations.AddAsync(recommendation, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<CopilotRecommendation?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _db.CopilotRecommendations.FindAsync(new object[] { id }, cancellationToken);

    public async Task<IReadOnlyList<CopilotRecommendation>> SearchAsync(
        Guid organizationId,
        RecommendationStatus? status = null,
        string? persona = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _db.CopilotRecommendations
            .Where(r => r.OrganizationId == organizationId);

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(persona))
            query = query.Where(r => r.Persona == persona);

        return await query
            .OrderByDescending(r => r.Priority)
            .ThenByDescending(r => r.GeneratedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task AcceptAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var recommendation = await _db.CopilotRecommendations.FindAsync(new object[] { id }, cancellationToken);
        if (recommendation is null)
            throw new InvalidOperationException($"CopilotRecommendation {id} not found.");

        recommendation.Accept();
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DismissAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var recommendation = await _db.CopilotRecommendations.FindAsync(new object[] { id }, cancellationToken);
        if (recommendation is null)
            throw new InvalidOperationException($"CopilotRecommendation {id} not found.");

        recommendation.Dismiss();
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ExpireAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var recommendation = await _db.CopilotRecommendations.FindAsync(new object[] { id }, cancellationToken);
        if (recommendation is null)
            throw new InvalidOperationException($"CopilotRecommendation {id} not found.");

        recommendation.Expire();
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task BulkCreateAsync(
        IReadOnlyList<CopilotRecommendation> recommendations,
        CancellationToken cancellationToken = default)
    {
        if (!recommendations.Any()) return;
        await _db.CopilotRecommendations.AddRangeAsync(recommendations, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ExpireStaleAsync(
        Guid organizationId,
        DateTime olderThan,
        CancellationToken cancellationToken = default)
    {
        var stale = await _db.CopilotRecommendations
            .Where(r => r.OrganizationId == organizationId
                     && r.Status == RecommendationStatus.Proposed
                     && r.GeneratedAtUtc < olderThan)
            .ToListAsync(cancellationToken);

        foreach (var r in stale)
            r.Expire();

        if (stale.Any())
            await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CopilotRecommendation>> GetByPersonaAsync(
        Guid organizationId,
        string persona,
        RecommendationStatus? status = null,
        int maxCount = 5,
        CancellationToken cancellationToken = default)
    {
        var query = _db.CopilotRecommendations
            .Where(r => r.OrganizationId == organizationId && r.Persona == persona);

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        return await query
            .OrderBy(r => r.Priority)
            .ThenByDescending(r => r.GeneratedAtUtc)
            .Take(maxCount)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await _db.SaveChangesAsync(cancellationToken);
}
