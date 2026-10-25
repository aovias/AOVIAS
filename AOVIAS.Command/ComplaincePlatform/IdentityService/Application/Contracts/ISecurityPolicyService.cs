using IdentityService.Domain.Entities;

namespace IdentityService.Application.Contracts;

public interface ISecurityPolicyService
{
    Task<UserSecurityPolicy> GetEffectivePolicyAsync(CancellationToken cancellationToken = default);
}
