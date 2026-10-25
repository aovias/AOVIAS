using System.Text;
using System.Text.Json;

namespace Gateway.Middleware;

/// <summary>
/// Middleware to provide detailed error messages for organization access authorization failures.
/// Intercepts 403 Forbidden responses and adds context about why organization access was denied.
/// </summary>
public class OrganizationAccessFailureMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<OrganizationAccessFailureMiddleware> _logger;

    public OrganizationAccessFailureMiddleware(
        RequestDelegate next,
        ILogger<OrganizationAccessFailureMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Capture original response body stream
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        await _next(context);

        // Only process 403 Forbidden responses on organization endpoints
        if (context.Response.StatusCode == StatusCodes.Status403Forbidden &&
            IsOrganizationEndpoint(context))
        {
            // Provide detailed error message based on authorization failure
            var errorMessage = GetOrganizationAccessErrorMessage(context);
            var reason = GetAccessDeniedReason(context);

            // Replace the response with detailed error
            responseBody.SetLength(0);
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status403Forbidden;

            var errorResponse = new
            {
                success = false,
                error = new
                {
                    message = errorMessage,
                    code = "ORGANIZATION_ACCESS_DENIED",
                    status = 403,
                    details = new
                    {
                        requestedOrgId = context.Request.RouteValues["orgId"]?.ToString(),
                        reason = reason
                    }
                }
            };

            var jsonResponse = JsonSerializer.Serialize(errorResponse);
            var jsonBytes = Encoding.UTF8.GetBytes(jsonResponse);
            await responseBody.WriteAsync(jsonBytes, 0, jsonBytes.Length);
        }

        // Copy response back to original stream
        responseBody.Seek(0, SeekOrigin.Begin);
        await responseBody.CopyToAsync(originalBodyStream);
    }

    private static bool IsOrganizationEndpoint(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        return path.Contains("/organizations/", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetOrganizationAccessErrorMessage(HttpContext context)
    {
        var user = context.User;
        var routeOrgId = context.Request.RouteValues["orgId"]?.ToString();

        // Check if user is authenticated
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            return "Authentication required to access organization resources.";
        }

        // Check if user is SUPER_ADMIN (should have passed, but just in case)
        if (user.IsInRole(Shared.Infrastructure.Auth.ApplicationRoles.SUPER_ADMIN))
        {
            return "Unexpected authorization failure for SUPER_ADMIN user.";
        }

        // Check if organization_id claim exists
        var orgIdClaim = user.FindFirst("organization_id")?.Value;
        if (string.IsNullOrEmpty(orgIdClaim))
        {
            return "Access denied: Your account is not associated with an organization. Please contact your administrator.";
        }

        // Check if organization_id matches route
        if (!Guid.TryParse(orgIdClaim, out var userOrgId))
        {
            return "Access denied: Invalid organization association in your account. Please contact your administrator.";
        }

        if (!Guid.TryParse(routeOrgId, out var requestedOrgId))
        {
            return "Access denied: Invalid organization ID in request.";
        }

        if (userOrgId != requestedOrgId)
        {
            return $"Access denied: You can only access your own organization's resources. " +
                   $"Requested organization: {requestedOrgId}, Your organization: {userOrgId}.";
        }

        // Generic fallback
        return "Access denied: You do not have permission to access this organization's resources.";
    }

    private static string GetAccessDeniedReason(HttpContext context)
    {
        var user = context.User;
        var routeOrgId = context.Request.RouteValues["orgId"]?.ToString();

        if (!user.Identity?.IsAuthenticated ?? true)
        {
            return "Not authenticated";
        }

        var orgIdClaim = user.FindFirst("organization_id")?.Value;
        if (string.IsNullOrEmpty(orgIdClaim))
        {
            return "Missing organization_id claim in JWT token";
        }

        if (!Guid.TryParse(orgIdClaim, out var userOrgId) || !Guid.TryParse(routeOrgId, out var requestedOrgId))
        {
            return "Invalid organization ID format";
        }

        if (userOrgId != requestedOrgId)
        {
            return "Organization ID mismatch - user's organization does not match requested organization";
        }

        return "Unknown authorization failure";
    }
}
