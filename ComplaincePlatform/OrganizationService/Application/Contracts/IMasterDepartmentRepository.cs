using Shared.Infrastructure.Domain;

namespace OrganizationService.Application.Contracts
{
    public interface IMasterDepartmentRepository : IRepository<MasterDepartment>
    {
        Task<List<MasterDepartment>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>Returns how many of the given IDs exist in MasterDepartments (any active state).</summary>
        Task<int> CountExistingIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);
    }
}

