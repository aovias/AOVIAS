using System.Security.Claims;
using ComplianceService.Application.Contracts;
using Microsoft.AspNetCore.Http;

namespace ComplianceService.Infrastructure.Services;

/// <summary>
/// Implementation of IUserContextService that extracts user context from JWT claims
/// </summary>
public class UserContextService : IUserContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<UserContextService> _logger;

    public UserContextService(IHttpContextAccessor httpContextAccessor, ILogger<UserContextService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public Guid OrganizationId
    {
        get
        {
            var claim = GetClaim("organization_id");
            if (string.IsNullOrEmpty(claim) || !Guid.TryParse(claim, out var orgId))
            {
                _logger.LogWarning("Missing or invalid organization_id claim in JWT token");
                throw new UnauthorizedAccessException("Missing or invalid organization_id claim in token");
            }
            return orgId;
        }
    }

    public Guid HospitalId
    {
        get
        {
            var claim = GetClaim("hospital_id");
            if (string.IsNullOrEmpty(claim) || !Guid.TryParse(claim, out var hospitalId))
            {
                _logger.LogWarning("Missing or invalid hospital_id claim in JWT token");
                throw new UnauthorizedAccessException("Missing or invalid hospital_id claim in token");
            }
            return hospitalId;
        }
    }

    public IReadOnlyList<string> Roles
    {
        get
        {
            var user = RequireAuthenticatedUser();
            var list = user.FindAll(ClaimTypes.Role)
                .Concat(user.FindAll("role"))
                .Select(c => c.Value)
                .Distinct(StringComparer.Ordinal)
                .ToList();
            return list;
        }
    }

    public string Role => Roles.FirstOrDefault() ?? string.Empty;

    public Guid UserId
    {
        get
        {
            var claim = GetClaim(ClaimTypes.NameIdentifier) ?? GetClaim("sub");
            if (string.IsNullOrEmpty(claim) || !Guid.TryParse(claim, out var userId))
            {
                _logger.LogWarning("Missing or invalid user ID claim in JWT token");
                throw new UnauthorizedAccessException("Missing or invalid user ID claim in token");
            }
            return userId;
        }
    }

    public string Email
    {
        get
        {
            var claim = GetClaim(ClaimTypes.Email) ?? GetClaim("email");
            return claim ?? string.Empty;
        }
    }

    private System.Security.Claims.ClaimsPrincipal RequireAuthenticatedUser()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null || user.Identity?.IsAuthenticated != true)
        {
            _logger.LogWarning("User is not authenticated");
            throw new UnauthorizedAccessException("User is not authenticated");
        }

        return user;
    }

    private string? GetClaim(string claimType)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null || user.Identity?.IsAuthenticated != true)
        {
            _logger.LogWarning("User is not authenticated");
            throw new UnauthorizedAccessException("User is not authenticated");
        }

        return user.FindFirst(claimType)?.Value;
    }
}
