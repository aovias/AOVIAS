using Microsoft.EntityFrameworkCore;
using OrganizationService.Application.Contracts;
using OrganizationService.Infrastructure.Persistence;
using Shared.Infrastructure.Domain;

namespace OrganizationService.Infrastructure.Repositories
{
    public class MasterDepartmentRepository : Repository<MasterDepartment>, IMasterDepartmentRepository
    {
        private readonly ILogger<MasterDepartmentRepository> _logger;

        public MasterDepartmentRepository(
            OrganizationDbContext db,
            ILogger<MasterDepartmentRepository> logger) : base(db)
        {
            _logger = logger;
        }

        public async Task<List<MasterDepartment>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Querying active master departments from database");

            var items = await _db.MasterDepartments
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name)
                .ToListAsync(cancellationToken);

            _logger.LogInformation(
                "Retrieved master departments. Count: {Count}",
                items.Count);

            return items;
        }

        public async Task<int> CountExistingIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
        {
            if (ids.Count == 0)
            {
                return 0;
            }

            return await _db.MasterDepartments
                .AsNoTracking()
                .CountAsync(m => ids.Contains(m.Id), cancellationToken);
        }
    }
}

