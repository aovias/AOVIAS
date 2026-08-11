using Shared.Infrastructure.Domain;

namespace OrganizationService.Application.Contracts
{
    public interface IServiceRepository : IRepository<Service>
    {
        Task<List<Service>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<List<Service>> GetActiveAsync(CancellationToken cancellationToken = default);
        Task<List<Service>> GetActiveByDepartmentAsync(Guid departmentId, CancellationToken cancellationToken = default);
    }
}
