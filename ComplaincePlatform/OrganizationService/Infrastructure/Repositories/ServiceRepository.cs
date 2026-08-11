using Microsoft.EntityFrameworkCore;
using OrganizationService.Application.Contracts;
using OrganizationService.Infrastructure.Persistence;
using Shared.Infrastructure.Domain;

namespace OrganizationService.Infrastructure.Repositories
{
    public class ServiceRepository : Repository<Service>, IServiceRepository
    {
        public ServiceRepository(OrganizationDbContext db) : base(db) { }

        public async Task<List<Service>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _db.Services
                .AsNoTracking()
                .OrderBy(s => s.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Service>> GetActiveAsync(CancellationToken cancellationToken = default)
        {
            return await _db.Services
                .AsNoTracking()
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Service>> GetActiveByDepartmentAsync(Guid departmentId, CancellationToken cancellationToken = default)
        {
            return await _db.Services
                .AsNoTracking()
                .Where(s => s.IsActive && s.DepartmentId == departmentId)
                .OrderBy(s => s.Name)
                .ToListAsync(cancellationToken);
        }
    }
}
