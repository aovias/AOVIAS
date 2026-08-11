using Microsoft.AspNetCore.Authorization;

namespace Shared.Infrastructure.Auth;

/// <summary>
/// Authorization requirement for scope-based access control
/// </summary>
public class ScopeRequirement : IAuthorizationRequirement
{
    public string RequiredScope { get; }

    public ScopeRequirement(string requiredScope)
    {
        RequiredScope = requiredScope ?? throw new ArgumentNullException(nameof(requiredScope));
    }
}

/// <summary>
/// Authorization handler for scope requirements
/// </summary>
public class ScopeHandler : AuthorizationHandler<ScopeRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ScopeRequirement requirement)
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            return Task.CompletedTask;
        }

        var scopeClaims = context.User.FindAll(AuthConstants.Claims.Scope);
        
        foreach (var claim in scopeClaims)
        {
            // Handle both single-value and space-separated scopes (OAuth2 standard)
            var scopes = claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (scopes.Contains(requirement.RequiredScope, StringComparer.OrdinalIgnoreCase))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }
        }

        return Task.CompletedTask;
    }
}
