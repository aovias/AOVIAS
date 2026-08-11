using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.Auth;

namespace Gateway.Authorization;

/// <summary>
/// Authorization handler for organization access control.
/// Validates that the user's organization_id claim matches the route's orgId parameter,
/// or that the user is a SUPER_ADMIN (who can access any organization).
/// 
/// This prevents multi-tenant data breaches in healthcare compliance systems.
/// </summary>
public class OrganizationAccessHandler : AuthorizationHandler<OrganizationAccessRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<OrganizationAccessHandler> _logger;

    public OrganizationAccessHandler(
        IHttpContextAccessor httpContextAccessor,
        ILogger<OrganizationAccessHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OrganizationAccessRequirement requirement)
    {
        // SUPER_ADMIN can access any organization
        if (context.User.IsInRole(ApplicationRoles.SUPER_ADMIN))
        {
            _logger.LogDebug("SUPER_ADMIN user - organization access granted");
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Extract orgId from route values using HttpContextAccessor (must be done first)
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            _logger.LogError("HttpContext is null in OrganizationAccessHandler");
            context.Fail();
            return Task.CompletedTask;
        }

        var routeData = httpContext.GetRouteData();
        // OrgAccess routes use orgId; tenant APIs use tenantId / organizationId.
        object? routeOrgIdValue = null;
        if (routeData?.Values is { } values)
        {
            if (!values.TryGetValue("orgId", out routeOrgIdValue) || routeOrgIdValue is null)
                values.TryGetValue("tenantId", out routeOrgIdValue);
            if (routeOrgIdValue is null)
                values.TryGetValue("organizationId", out routeOrgIdValue);
        }

        if (routeOrgIdValue is null)
        {
            _logger.LogWarning("Route does not contain orgId/tenantId/organizationId parameter");
            context.Fail();
            return Task.CompletedTask;
        }

        // Parse route orgId
        if (!Guid.TryParse(routeOrgIdValue?.ToString(), out var routeOrgId))
        {
            _logger.LogWarning("Route orgId parameter is not a valid GUID: {RouteOrgId}", routeOrgIdValue);
            context.Fail();
            return Task.CompletedTask;
        }

        // Extract organization_id from JWT token
        var orgIdClaim = context.User.FindFirst("organization_id")?.Value;
        if (string.IsNullOrEmpty(orgIdClaim) || !Guid.TryParse(orgIdClaim, out var userOrgId))
        {
            var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            _logger.LogWarning(
                "Organization access denied: Missing or invalid organization_id claim in JWT token. User: {UserId}, Route OrgId: {RouteOrgId}",
                userId, routeOrgId);
            
            // Store failure reason in HttpContext for middleware to use
            httpContext.Items["OrgAccessFailureReason"] = "Missing or invalid organization_id claim";
            
            context.Fail();
            return Task.CompletedTask;
        }

        // Validate that the requested organization ID matches the token's organization ID
        if (routeOrgId != userOrgId)
        {
            var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            _logger.LogWarning(
                "Organization access denied: Organization ID mismatch. User: {UserId}, Requested OrgId: {RequestedOrgId}, User's OrgId: {UserOrgId}",
                userId, routeOrgId, userOrgId);
            
            // Store failure reason in HttpContext for middleware to use
            httpContext.Items["OrgAccessFailureReason"] = "Organization ID mismatch";
            httpContext.Items["UserOrgId"] = userOrgId.ToString();
            httpContext.Items["RequestedOrgId"] = routeOrgId.ToString();
            
            context.Fail();
            return Task.CompletedTask;
        }

        // Validation passed
        _logger.LogDebug("Organization access validated: User {UserId} accessing Org {OrgId}", 
            context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown", 
            routeOrgId);
        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
