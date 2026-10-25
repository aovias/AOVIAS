using OrganizationService.Application.Contracts;
using OrganizationService.Infrastructure.Persistence;

namespace OrganizationService.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly OrganizationDbContext _db;
        public UnitOfWork(OrganizationDbContext db) => _db = db;
        public Task<int> SaveChangesAsync() => _db.SaveChangesAsync();
    }
}
