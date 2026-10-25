using Microsoft.EntityFrameworkCore;
using OrganizationService.Application.Contracts;
using OrganizationService.Infrastructure.Persistence;
using Shared.Infrastructure.Domain;

namespace OrganizationService.Infrastructure.Repositories;

public class HospitalRepository : Repository<Hospital>, IHospitalRepository
{
    public HospitalRepository(OrganizationDbContext db) : base(db) { }

    public async Task<Hospital?> GetByCodeAsync(string code, Guid organizationId)
    {
        return await _db.Hospitals
            .FirstOrDefaultAsync(h => h.Code == code && h.OrganizationId == organizationId);
    }

    public async Task<List<Hospital>> ListByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _db.Hospitals
            .Where(h => h.OrganizationId == organizationId)
            .OrderBy(h => h.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
