using IdentityService.Application.Contracts;
using IdentityService.Infrastructure;
using IdentityService.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Application.Services;

public class RevokedAccessTokenService : IRevokedAccessTokenService
{
    private readonly IdentityDbContext _db;

    public RevokedAccessTokenService(IdentityDbContext db)
    {
        _db = db;
    }

    public async Task RevokeAsync(string jti, Guid? userId, DateTime expiresAt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jti))
            return;

        var exists = await _db.RevokedAccessTokens.AnyAsync(x => x.Jti == jti, cancellationToken);
        if (exists)
            return;

        _db.RevokedAccessTokens.Add(new Domain.Entities.RevokedAccessToken
        {
            Id = Guid.NewGuid(),
            Jti = jti,
            UserId = userId,
            ExpiresAt = expiresAt,
            RevokedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jti))
            return false;

        return await _db.RevokedAccessTokens.AnyAsync(x => x.Jti == jti && x.ExpiresAt > DateTime.UtcNow, cancellationToken);
    }
}

public class IdentityUnitOfWork : IIdentityUnitOfWork
{
    private readonly IdentityDbContext _db;

    public IdentityUnitOfWork(IdentityDbContext db) => _db = db;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _db.SaveChangesAsync(cancellationToken);
}
