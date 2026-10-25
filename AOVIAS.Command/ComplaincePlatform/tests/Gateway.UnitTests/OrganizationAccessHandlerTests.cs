using System.Security.Claims;
using Gateway.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Infrastructure.Auth;
using Xunit;

namespace Gateway.UnitTests;

/// <summary>
/// Tenant isolation unit tests for Gateway OrgAccess (OrganizationId claim vs route orgId).
/// </summary>
public class OrganizationAccessHandlerTests
{
    private static async Task<AuthorizationHandlerContext> EvaluateAsync(
        ClaimsPrincipal user,
        Guid? routeOrgId)
    {
        var httpContext = new DefaultHttpContext();
        if (routeOrgId.HasValue)
        {
            var routeData = new RouteData();
            routeData.Values["orgId"] = routeOrgId.Value.ToString();
            httpContext.Features.Set<IRoutingFeature>(new RoutingFeature { RouteData = routeData });
        }

        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var handler = new OrganizationAccessHandler(accessor, NullLogger<OrganizationAccessHandler>.Instance);
        var requirement = new OrganizationAccessRequirement();
        var context = new AuthorizationHandlerContext([requirement], user, resource: null);
        await handler.HandleAsync(context);
        return context;
    }

    private static ClaimsPrincipal UserWithOrg(Guid organizationId, params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString("N")),
            new("organization_id", organizationId.ToString())
        };
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Test"));
    }

    [Fact]
    public async Task Matching_organization_id_succeeds()
    {
        var orgId = Guid.NewGuid();
        var context = await EvaluateAsync(UserWithOrg(orgId), orgId);
        Assert.True(context.HasSucceeded);
        Assert.False(context.HasFailed);
    }

    [Fact]
    public async Task Mismatched_organization_id_fails()
    {
        var userOrg = Guid.NewGuid();
        var routeOrg = Guid.NewGuid();
        var context = await EvaluateAsync(UserWithOrg(userOrg), routeOrg);
        Assert.True(context.HasFailed);
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Super_admin_succeeds_for_any_organization()
    {
        var userOrg = Guid.NewGuid();
        var otherOrg = Guid.NewGuid();
        var user = UserWithOrg(userOrg, ApplicationRoles.SUPER_ADMIN);
        var context = await EvaluateAsync(user, otherOrg);
        Assert.True(context.HasSucceeded);
        Assert.False(context.HasFailed);
    }

    [Fact]
    public async Task Missing_organization_claim_fails()
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString("N"))
        ], "Test");
        var context = await EvaluateAsync(new ClaimsPrincipal(identity), Guid.NewGuid());
        Assert.True(context.HasFailed);
    }
}
