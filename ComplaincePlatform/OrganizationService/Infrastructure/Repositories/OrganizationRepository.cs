using Microsoft.EntityFrameworkCore;
using OrganizationService.Application.Contracts;
using OrganizationService.Infrastructure.Persistence;
using Shared.Infrastructure.Domain;

namespace OrganizationService.Infrastructure.Repositories
{
    public class OrganizationRepository : Repository<Organization>, IOrganizationRepository
    {
        public OrganizationRepository(OrganizationDbContext db) : base(db) { }

        public async Task<List<Organization>> GetByUserAsync(Guid userId)
        {
            return await (from m in _db.Members
                          join o in _db.Organizations on m.OrganizationId equals o.Id
                          where m.UserId == userId
                          select o).ToListAsync();
        }
    }
}
