using IdentityService.Application.Contracts;
using IdentityService.Domain;
using IdentityService.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace IdentityService.Application.Services;

public class TokenValidationService : ITokenValidationService
{
    private readonly IRevokedAccessTokenService _revokedTokens;
    private readonly UserManager<AppUser> _userManager;

    public TokenValidationService(
        IRevokedAccessTokenService revokedTokens,
        UserManager<AppUser> userManager)
    {
        _revokedTokens = revokedTokens;
        _userManager = userManager;
    }

    public async Task<bool> ValidateAsync(
        Guid userId,
        string jti,
        int tokenVersion,
        string securityStamp,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jti))
            return false;

        if (await _revokedTokens.IsRevokedAsync(jti, cancellationToken))
            return false;

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null || user.IsDeleted)
            return false;

        if (user.Status is UserAccountStatus.Suspended
            or UserAccountStatus.Disabled
            or UserAccountStatus.Archived
            or UserAccountStatus.Deleted
            or UserAccountStatus.PendingVerification)
            return false;

        if (user.TokenVersion != tokenVersion)
            return false;

        var stamp = await _userManager.GetSecurityStampAsync(user) ?? string.Empty;
        if (!string.Equals(stamp, securityStamp, StringComparison.Ordinal))
            return false;

        return true;
    }
}
