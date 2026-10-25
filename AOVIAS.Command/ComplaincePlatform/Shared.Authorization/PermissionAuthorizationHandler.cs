using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Shared.Authorization;

public sealed class PermissionAuthorizationHandler(
    IAuthorizationEvaluator evaluator,
    ILogger<PermissionAuthorizationHandler> logger) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return;

        var httpContext = context.Resource as HttpContext;
        var extraContext = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (httpContext != null)
        {
            foreach (var routeValue in httpContext.Request.RouteValues)
            {
                if (routeValue.Value != null)
                    extraContext[$"route_{routeValue.Key}"] = routeValue.Value.ToString()!;
            }
        }

        var request = AuthorizationContextBuilder.FromPermissionCode(context.User, requirement.PermissionCode, extraContext);
        var decision = await evaluator.EvaluateAsync(request, httpContext?.RequestAborted ?? CancellationToken.None);
        if (decision.Allowed)
        {
            context.Succeed(requirement);
            return;
        }

        logger.LogDebug("Permission denied for {PermissionCode}: {Reason}", requirement.PermissionCode, decision.Reason);
    }
}
