namespace IdentityService.Application.Contracts;

public interface ITokenValidationService
{
    Task<bool> ValidateAsync(
        Guid userId,
        string jti,
        int tokenVersion,
        string securityStamp,
        CancellationToken cancellationToken = default);
}
