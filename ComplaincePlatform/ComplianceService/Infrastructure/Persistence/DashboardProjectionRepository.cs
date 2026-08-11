using ComplianceService.Domain;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Infrastructure.Persistence;

public sealed class DashboardProjectionRepository : Application.Dashboard.Projection.IDashboardProjectionRepository
{
    private readonly ComplianceDbContext _dbContext;

    public DashboardProjectionRepository(ComplianceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DashboardProjection?> GetAsync(
        Application.Dashboard.Projection.DashboardProjectionScope scope,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.DashboardProjections
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.OrganizationId == scope.OrganizationId
                     && p.HospitalId == scope.HospitalId
                     && p.FrameworkId == scope.FrameworkId,
                cancellationToken);
    }

    public async Task<DashboardProjection> GetOrCreateAsync(
        Application.Dashboard.Projection.DashboardProjectionScope scope,
        CancellationToken cancellationToken = default)
    {
        // DEF-M5-005 — check the change tracker before the database.
        //
        // {OrganizationId, HospitalId, FrameworkId} is unique. A database query cannot see an
        // instance this unit of work has already Added but not yet saved, so processing several
        // outbox messages for one scope used to mint a second and third DashboardProjection for the
        // same key. SaveChanges then had to order competing writes against the unique index and
        // reported a circular dependency, failing the whole request.
        var local = _dbContext.DashboardProjections.Local
            .FirstOrDefault(
                p => p.OrganizationId == scope.OrganizationId
                     && p.HospitalId == scope.HospitalId
                     && p.FrameworkId == scope.FrameworkId);

        if (local is not null)
        {
            return local;
        }

        var existing = await _dbContext.DashboardProjections
            .FirstOrDefaultAsync(
                p => p.OrganizationId == scope.OrganizationId
                     && p.HospitalId == scope.HospitalId
                     && p.FrameworkId == scope.FrameworkId,
                cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var created = new DashboardProjection
        {
            Id = Guid.NewGuid(),
            OrganizationId = scope.OrganizationId,
            HospitalId = scope.HospitalId,
            FrameworkId = scope.FrameworkId,
            ProjectionSchemaVersion = Application.Dashboard.Projection.DashboardProjectionPayload.CurrentSchemaVersion,
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow,
        };

        await _dbContext.DashboardProjections.AddAsync(created, cancellationToken);
        return created;
    }

    public async Task SaveAsync(DashboardProjection projection, CancellationToken cancellationToken = default)
    {
        projection.LastUpdatedAt = DateTime.UtcNow;

        // DEF-M5-005 — the projection comes from GetOrCreateAsync and is already tracked, as either
        // Added or Modified. Calling Update() here forced Added instances to Modified, turning a
        // pending insert into an update of a row that does not exist yet. EF change tracking picks
        // the right operation on its own.
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkCorruptedAsync(
        Application.Dashboard.Projection.DashboardProjectionScope scope,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var projection = await _dbContext.DashboardProjections
            .FirstOrDefaultAsync(
                p => p.OrganizationId == scope.OrganizationId
                     && p.HospitalId == scope.HospitalId
                     && p.FrameworkId == scope.FrameworkId,
                cancellationToken);

        if (projection is null)
        {
            return;
        }

        projection.IsCorrupted = true;
        projection.IsComplete = false;
        projection.CorruptionReason = reason;
        projection.LastUpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DashboardProjection>> ListByHospitalAsync(
        Guid organizationId,
        Guid hospitalId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.DashboardProjections
            .AsNoTracking()
            .Where(p => p.OrganizationId == organizationId && p.HospitalId == hospitalId)
            .ToListAsync(cancellationToken);
    }
}
