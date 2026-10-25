using IdentityService.Application.Contracts;
using IdentityService.Domain.Entities;
using IdentityService.Infrastructure;
using IdentityService.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Encodings.Web;

namespace IdentityService.Application.Services;

public class MfaService : IMfaService
{
    private const string AuthenticatorUriFormat = "otpauth://totp/PRAMAN:{0}?secret={1}&issuer=PRAMAN&digits=6";
    private readonly UserManager<Domain.AppUser> _userManager;
    private readonly IdentityDbContext _db;
    private readonly UrlEncoder _urlEncoder;

    public MfaService(UserManager<Domain.AppUser> userManager, IdentityDbContext db, UrlEncoder urlEncoder)
    {
        _userManager = userManager;
        _db = db;
        _urlEncoder = urlEncoder;
    }

    public async Task<(bool success, string? qrUri, string? sharedKey, string? message)> SetupAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return (false, null, null, "User not found.");

        await _userManager.ResetAuthenticatorKeyAsync(user);
        var key = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(key))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            key = await _userManager.GetAuthenticatorKeyAsync(user);
        }

        var email = user.Email ?? user.UserName ?? userId.ToString();
        var qrUri = string.Format(AuthenticatorUriFormat, _urlEncoder.Encode(email), key);
        return (true, qrUri, key, null);
    }

    public async Task<(bool success, string? message)> VerifySetupAsync(Guid userId, string code, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return (false, "User not found.");

        var valid = await _userManager.VerifyTwoFactorTokenAsync(user, _userManager.Options.Tokens.AuthenticatorTokenProvider, code);
        if (!valid)
            return (false, "Invalid verification code.");

        await _userManager.SetTwoFactorEnabledAsync(user, true);
        return (true, null);
    }

    public async Task<(bool success, string? message)> DisableAsync(Guid userId, string password, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return (false, "User not found.");

        if (!await _userManager.CheckPasswordAsync(user, password))
            return (false, "Invalid password.");

        await _userManager.SetTwoFactorEnabledAsync(user, false);
        await _userManager.ResetAuthenticatorKeyAsync(user);
        return (true, null);
    }

    public async Task<IReadOnlyList<string>> GetRecoveryCodesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return Array.Empty<string>();

        var codes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        return codes?.ToList() ?? new List<string>();
    }

    public async Task<(bool success, string? message)> VerifyLoginCodeAsync(Guid userId, string code, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return (false, "User not found.");

        var valid = await _userManager.VerifyTwoFactorTokenAsync(user, _userManager.Options.Tokens.AuthenticatorTokenProvider, code);
        if (valid)
            return (true, null);

        var recovery = await _userManager.RedeemTwoFactorRecoveryCodeAsync(user, code);
        return recovery.Succeeded ? (true, null) : (false, "Invalid MFA code.");
    }

    public async Task<string?> CreateLoginChallengeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var token = TokenHasher.GenerateOpaqueToken();
        _db.MfaChallenges.Add(new MfaChallenge
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ChallengeTokenHash = TokenHasher.Hash(token),
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
        return token;
    }

    public async Task<Guid?> ValidateLoginChallengeAsync(string challengeToken, CancellationToken cancellationToken = default)
    {
        var hash = TokenHasher.Hash(challengeToken);
        var challenge = await _db.MfaChallenges
            .FirstOrDefaultAsync(c => c.ChallengeTokenHash == hash && c.UsedAt == null && c.ExpiresAt > DateTime.UtcNow, cancellationToken);
        if (challenge == null)
            return null;

        challenge.UsedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return challenge.UserId;
    }
}
