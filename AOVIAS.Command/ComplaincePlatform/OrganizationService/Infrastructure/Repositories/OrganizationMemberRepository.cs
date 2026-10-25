using Microsoft.EntityFrameworkCore;
using OrganizationService.Application.Contracts;
using OrganizationService.Infrastructure.Persistence;
using Shared.Infrastructure.Domain;

namespace OrganizationService.Infrastructure.Repositories;

public class OrganizationMemberRepository : Repository<OrganizationMember>, IOrganizationMemberRepository
{
    public OrganizationMemberRepository(OrganizationDbContext db) : base(db) { }

    public async Task<OrganizationMember?> GetByUserAndOrganizationAsync(Guid userId, Guid organizationId)
    {
        return await _db.Members
            .FirstOrDefaultAsync(m => m.UserId == userId && m.OrganizationId == organizationId);
    }

    public async Task<OrganizationMember?> GetByIdAsync(Guid memberId, CancellationToken cancellationToken = default)
    {
        return await _db.Members.FirstOrDefaultAsync(m => m.Id == memberId, cancellationToken);
    }

    public async Task<List<OrganizationMember>> ListByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _db.Members
            .Where(m => m.OrganizationId == organizationId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<OrganizationMember?> GetByEmailAsync(Guid organizationId, string email, Guid? excludeMemberId = null, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        return await _db.Members.FirstOrDefaultAsync(
            m => m.OrganizationId == organizationId &&
                 m.Email != null &&
                 m.Email.ToLower() == normalizedEmail &&
                 (!excludeMemberId.HasValue || m.Id != excludeMemberId.Value),
            cancellationToken);
    }
}
