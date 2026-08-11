namespace IdentityService.Application.Contracts;

public interface IRevokedAccessTokenService
{
    Task RevokeAsync(string jti, Guid? userId, DateTime expiresAt, CancellationToken cancellationToken = default);
    Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken = default);
}
