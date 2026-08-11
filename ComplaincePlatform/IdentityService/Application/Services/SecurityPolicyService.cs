using IdentityService.Application.Contracts;
using IdentityService.Domain.Entities;
using IdentityService.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Application.Services;

public class SecurityPolicyService : ISecurityPolicyService
{
    private readonly IdentityDbContext _db;

    public SecurityPolicyService(IdentityDbContext db)
    {
        _db = db;
    }

    public async Task<UserSecurityPolicy> GetEffectivePolicyAsync(CancellationToken cancellationToken = default)
    {
        var policy = await _db.UserSecurityPolicies
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .FirstOrDefaultAsync(cancellationToken);

        if (policy != null)
            return policy;

        // Phase 7.1 — enterprise default: require MFA for administrator roles.
        policy = new UserSecurityPolicy
        {
            Id = Guid.NewGuid(),
            Name = "Default",
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            RequireMfaForAdminRoles = true,
            RequireMfaOnUnknownDevice = true
        };

        _db.UserSecurityPolicies.Add(policy);
        await _db.SaveChangesAsync(cancellationToken);
        return policy;
    }
}
