using ComplianceService.Application.Copilot.Contracts;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;
using ComplianceService.Infrastructure.Persistence;
using ComplianceService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Infrastructure.Copilot.Repositories;

public sealed class AiDraftRepository : Repository<AiDraft>, IAiDraftRepository
{
    public AiDraftRepository(ComplianceDbContext db) : base(db) { }

    public async Task CreateAsync(AiDraft draft, CancellationToken cancellationToken = default)
    {
        await _db.AiDrafts.AddAsync(draft, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<AiDraft?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _db.AiDrafts.FindAsync(new object[] { id }, cancellationToken);

    public async Task<IReadOnlyList<AiDraft>> SearchAsync(
        Guid organizationId,
        DraftStatus? status = null,
        string? persona = null,
        DraftType? draftType = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _db.AiDrafts
            .Where(d => d.OrganizationId == organizationId);

        if (status.HasValue)
            query = query.Where(d => d.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(persona))
            query = query.Where(d => d.Persona == persona);

        if (draftType.HasValue)
            query = query.Where(d => d.DraftType == draftType.Value);

        return await query
            .OrderByDescending(d => d.UpdatedUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(AiDraft draft, CancellationToken cancellationToken = default)
    {
        _db.AiDrafts.Update(draft);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var draft = await _db.AiDrafts.FindAsync(new object[] { id }, cancellationToken);
        if (draft is null)
            throw new InvalidOperationException($"AiDraft {id} not found.");

        draft.Archive();
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Returns all versions of the same logical draft group, ordered by version descending.</summary>
    public async Task<IReadOnlyList<AiDraft>> GetVersionsByGroupAsync(
        Guid groupId,
        CancellationToken cancellationToken = default) =>
        await _db.AiDrafts
            .Where(d => d.GroupId == groupId)
            .OrderByDescending(d => d.Version)
            .ToListAsync(cancellationToken);

    /// <summary>Legacy: returns drafts by organization + title match.</summary>
    public async Task<IReadOnlyList<AiDraft>> GetVersionHistoryAsync(
        Guid organizationId,
        string title,
        CancellationToken cancellationToken = default) =>
        await _db.AiDrafts
            .Where(d => d.OrganizationId == organizationId && d.Title == title)
            .OrderByDescending(d => d.Version)
            .ToListAsync(cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await _db.SaveChangesAsync(cancellationToken);
}
