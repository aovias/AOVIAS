using AIOrchestratorService.Domain;
using System.Security.Claims;

namespace AIOrchestratorService.Application.Context;

public interface IRequestContextBuilder
{
    RequestContext Build();
}

public class RequestContextBuilder : IRequestContextBuilder
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RequestContextBuilder(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public RequestContext Build()
    {
        var context = new RequestContext();
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            var claims = httpContext.User.Claims;

            // Extract UserId from "sub" claim
            var subClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "sub");
            if (subClaim != null && Guid.TryParse(subClaim.Value, out var userId))
            {
                context.UserId = userId;
            }

            // Extract Email
            var emailClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email || c.Type == "email");
            context.Email = emailClaim?.Value;

            // Extract OrganizationId (assuming custom claim "organization_id" or "org_id")
            var orgIdClaim = claims.FirstOrDefault(c => c.Type == "organization_id" || c.Type == "org_id");
            if (orgIdClaim != null && Guid.TryParse(orgIdClaim.Value, out var orgId))
            {
                context.OrganizationId = orgId;
            }

            // Extract Roles
            var roleClaims = claims.Where(c => c.Type == ClaimTypes.Role || c.Type == "role");
            context.Roles = roleClaims.Select(c => c.Value).ToList();

            // Extract Scopes (can be space-separated in a single claim or multiple claims)
            var scopeClaims = claims.Where(c => c.Type == "scope");
            var allScopes = new List<string>();
            foreach (var scopeClaim in scopeClaims)
            {
                // Scopes can be space-separated
                var scopes = scopeClaim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                allScopes.AddRange(scopes);
            }
            context.Scopes = allScopes.Distinct().ToList();
        }

        // Extract CorrelationId from headers
        if (httpContext?.Request != null)
        {
            var correlationId = httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                ?? httpContext.Request.Headers["Correlation-Id"].FirstOrDefault()
                ?? httpContext.Request.Headers["x-correlation-id"].FirstOrDefault();

            context.CorrelationId = correlationId;
        }

        return context;
    }
}
