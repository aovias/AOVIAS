using ComplianceService.Application.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Shared.Infrastructure.Api;

namespace ComplianceService.Infrastructure.Filters;

/// <summary>
/// Enforces hospital existence, organization ownership, and caller hospital authorization
/// whenever an action is parameterized by HospitalId (route, query, or body).
/// </summary>
public sealed class HospitalAccessActionFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!Authorization.HospitalIdLocator.TryGet(context, out var hospitalId))
        {
            await next();
            return;
        }

        var authorizer = context.HttpContext.RequestServices.GetRequiredService<IHospitalAccessAuthorizer>();
        var callerResolver = context.HttpContext.RequestServices.GetRequiredService<IHospitalAccessCallerResolver>();
        var accessContext = context.HttpContext.RequestServices.GetRequiredService<IHospitalAccessContext>();

        var result = await authorizer.AuthorizeAsync(hospitalId, callerResolver.Resolve(), context.HttpContext.RequestAborted);
        accessContext.Current = result;

        if (!result.IsAllowed)
        {
            var traceId = context.HttpContext.TraceIdentifier;
            context.Result = ApiResponseFactory.FailureResult<object>(result.ToApiResponseCode(), traceId);
            return;
        }

        await next();
    }
}

