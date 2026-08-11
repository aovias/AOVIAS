using IdentityService.Application.Contracts;
using IdentityService.Application.DTOs;
using IdentityService.Infrastructure;
using IdentityService.Infrastructure.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Shared.Infrastructure.Auth;

namespace IdentityService.Application.Services;

public interface ITokenService
{
    Task<TokenPairResult> GenerateTokenPairAsync(
        TokenClaimsContext context,
        string refreshToken,
        CancellationToken cancellationToken = default);

    string GenerateRefreshToken();
    string? ExtractJti(string accessToken);
    DateTime? ExtractExpiry(string accessToken);
}

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<TokenPairResult> GenerateTokenPairAsync(
        TokenClaimsContext context,
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JwtSettings:SecretKey is required");
        var issuer = jwtSettings["Issuer"] ?? throw new InvalidOperationException("JwtSettings:Issuer is required");
        var audience = jwtSettings["Audience"] ?? throw new InvalidOperationException("JwtSettings:Audience is required");
        var expirationMinutes = IdentitySecurityPolicyState.Current.AccessTokenExpirationMinutes;
        if (expirationMinutes <= 0)
            expirationMinutes = jwtSettings.GetValue<int>("ExpirationMinutes", 60);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var jti = Guid.NewGuid().ToString();
        var canonicalRoles = RoleLegacyMap.NormalizeRoleNames(context.Roles);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, context.UserId.ToString()),
            new(JwtRegisteredClaimNames.Email, context.Email),
            new(JwtRegisteredClaimNames.PreferredUsername, context.UserName),
            new(ClaimTypes.NameIdentifier, context.UserId.ToString()),
            new(ClaimTypes.Email, context.Email),
            new(JwtRegisteredClaimNames.Jti, jti),
            new(AuthConstants.Claims.PlatformId, context.PlatformId),
            new("organization_id", context.AuthContext.OrganizationId.ToString()),
            new("hospital_id", context.AuthContext.HospitalId.ToString()),
            new(AuthConstants.Claims.MembershipId, context.AuthContext.MembershipId.ToString()),
            new(AuthConstants.Claims.SessionId, context.SessionId.ToString()),
            new(AuthConstants.Claims.TokenVersion, context.TokenVersion.ToString()),
            new(AuthConstants.Claims.SecurityStamp, context.SecurityStamp),
            new(AuthConstants.Claims.AuthenticationMethod, context.AuthenticationMethod.ToString()),
            new(AuthConstants.Claims.AiEnabled, "false"),
            new(AuthConstants.Claims.AutomationLevel, "none"),
            new(AuthConstants.Claims.AssistantScope, "read")
        };

        if (context.AuthContext.DepartmentId.HasValue)
        {
            claims.Add(new Claim(AuthConstants.Claims.ActiveDepartmentId, context.AuthContext.DepartmentId.Value.ToString()));
        }

        foreach (var role in canonicalRoles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        if (context.PersonId.HasValue)
            claims.Add(new Claim(AuthConstants.Claims.PersonId, context.PersonId.Value.ToString()));

        if (context.EmploymentId.HasValue)
            claims.Add(new Claim(AuthConstants.Claims.EmploymentId, context.EmploymentId.Value.ToString()));

        if (context.ProfessionalProfileId.HasValue)
            claims.Add(new Claim(AuthConstants.Claims.ProfessionalProfileId, context.ProfessionalProfileId.Value.ToString()));

        if (context.Scopes.Count > 0)
            claims.Add(new Claim(AuthConstants.Claims.Scope, string.Join(" ", context.Scopes)));

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var expiresIn = expirationMinutes * 60;

        return Task.FromResult(new TokenPairResult
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = expiresIn,
            SessionId = context.SessionId,
            AccessTokenJti = jti
        });
    }

    public string GenerateRefreshToken()
        => Infrastructure.Security.TokenHasher.GenerateOpaqueToken();

    public string? ExtractJti(string accessToken)
    {
        var handler = new JwtSecurityTokenHandler();
        if (!handler.CanReadToken(accessToken))
            return null;
        return handler.ReadJwtToken(accessToken).Id;
    }

    public DateTime? ExtractExpiry(string accessToken)
    {
        var handler = new JwtSecurityTokenHandler();
        if (!handler.CanReadToken(accessToken))
            return null;
        return handler.ReadJwtToken(accessToken).ValidTo;
    }
}
