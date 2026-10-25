using Shared.Infrastructure.Domain;

namespace OrganizationService.Application.Contracts
{
    public interface IDepartmentRepository : IRepository<Department>
    {
        Task<List<Department>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<List<Department>> GetActiveAsync(CancellationToken cancellationToken = default);
    }
}
