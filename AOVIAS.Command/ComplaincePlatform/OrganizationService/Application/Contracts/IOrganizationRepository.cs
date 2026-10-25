using Shared.Infrastructure.Domain;

namespace OrganizationService.Application.Contracts
{
    public interface IOrganizationRepository : IRepository<Organization>
    {
        Task<List<Organization>> GetByUserAsync(Guid userId);
    }
}
