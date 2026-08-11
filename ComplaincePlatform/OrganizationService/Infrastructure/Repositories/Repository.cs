using OrganizationService.Application.Contracts;
using OrganizationService.Infrastructure.Persistence;

namespace OrganizationService.Infrastructure.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly OrganizationDbContext _db;

        public Repository(OrganizationDbContext db)
        {
            _db = db;
        }

        public async Task<T?> GetAsync(Guid id) => await _db.Set<T>().FindAsync(id);
        public async Task AddAsync(T entity) => await _db.Set<T>().AddAsync(entity);
        public void Update(T entity) => _db.Set<T>().Update(entity);
        public void Remove(T entity) => _db.Set<T>().Remove(entity);
        public IQueryable<T> Query() => _db.Set<T>();
    }
}
