using Shared.Infrastructure.Domain;

namespace OrganizationService.Application.Contracts;

public interface IHospitalRepository : IRepository<Hospital>
{
    Task<Hospital?> GetByCodeAsync(string code, Guid organizationId);
    Task<List<Hospital>> ListByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);
}
