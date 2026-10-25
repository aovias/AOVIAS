using Shared.Infrastructure.Domain;

namespace OrganizationService.Application.Contracts;

public interface IOrganizationMemberRepository : IRepository<OrganizationMember>
{
    Task<OrganizationMember?> GetByUserAndOrganizationAsync(Guid userId, Guid organizationId);
    Task<OrganizationMember?> GetByIdAsync(Guid memberId, CancellationToken cancellationToken = default);
    Task<List<OrganizationMember>> ListByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<OrganizationMember?> GetByEmailAsync(Guid organizationId, string email, Guid? excludeMemberId = null, CancellationToken cancellationToken = default);
}
