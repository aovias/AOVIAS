using Microsoft.AspNetCore.Authorization;

namespace Shared.Infrastructure.Auth;

/// <summary>
/// Succeeds when the authenticated user has <b>any</b> of the listed OAuth2-style API scopes (space-separated <c>scope</c> claim).
/// </summary>
public sealed class AnyScopeRequirement : IAuthorizationRequirement
{
    public IReadOnlyList<string> AnyOfScopes { get; }

    public AnyScopeRequirement(params string[] anyOfScopes)
    {
        AnyOfScopes = anyOfScopes ?? throw new ArgumentNullException(nameof(anyOfScopes));
    }
}

/// <summary>
/// Handler for <see cref="AnyScopeRequirement"/>.
/// </summary>
public sealed class AnyScopeHandler : AuthorizationHandler<AnyScopeRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AnyScopeRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        var scopeClaims = context.User.FindAll(AuthConstants.Claims.Scope);
        foreach (var claim in scopeClaims)
        {
            var scopes = claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var needed in requirement.AnyOfScopes)
            {
                if (scopes.Contains(needed, StringComparer.OrdinalIgnoreCase))
                {
                    context.Succeed(requirement);
                    return Task.CompletedTask;
                }
            }
        }

        return Task.CompletedTask;
    }
}
