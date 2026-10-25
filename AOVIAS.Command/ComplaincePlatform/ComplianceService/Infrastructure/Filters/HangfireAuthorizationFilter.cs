using Hangfire.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Shared.Infrastructure.Auth;

namespace ComplianceService.Infrastructure.Filters;

/// <summary>
/// Authorization filter for Hangfire Dashboard.
/// Requires user to be authenticated and have ComplianceScope policy.
/// </summary>
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        
        // Check if user is authenticated
        if (httpContext.User?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        // Check if user has ComplianceScope policy (requires "compliance.api" scope)
        var authorizationService = httpContext.RequestServices.GetRequiredService<IAuthorizationService>();
        var authorizationResult = authorizationService.AuthorizeAsync(
            httpContext.User,
            null,
            AuthConstants.Policies.ComplianceScope).GetAwaiter().GetResult();

        return authorizationResult.Succeeded;
    }
}
