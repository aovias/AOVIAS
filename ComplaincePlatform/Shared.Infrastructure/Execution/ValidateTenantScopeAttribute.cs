using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.Api;

namespace Shared.Infrastructure.Execution;

/// <summary>
/// Enforces that an action's <c>organizationId</c> argument matches the caller's authenticated
/// tenant, and rewrites it to the authoritative value before the action runs.
///
/// <para>
/// **Why a filter rather than per-action checks.** 24 endpoints across Workforce Intelligence and
/// Enterprise Directory accept <c>organizationId</c> from the query string behind nothing but a
/// permission check (Wave 2.2A, risk R-02). Hand-editing each one duplicates the same five lines 24
/// times and leaves the next new endpoint unprotected by default. Applied at controller level this
/// is one implementation, and it covers actions added later.
/// </para>
///
/// <para>
/// **Rewriting matters as much as rejecting.** On success the argument is replaced with the value
/// derived from the token, so an action cannot act on a client-supplied tenant even if the
/// comparison is later loosened.
/// </para>
///
/// <para>
/// Set <see cref="CrossTenant"/> on endpoints that are cross-tenant by design (staff consoles);
/// those still require a cross-tenant role.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class ValidateTenantScopeAttribute : Attribute, IActionFilter
{
    /// <summary>Name of the action argument carrying the organization id.</summary>
    public string ParameterName { get; set; } = "organizationId";

    /// <summary>True for endpoints intended to operate across tenants (AOVIAS staff consoles).</summary>
    public bool CrossTenant { get; set; }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        // Actions with no organization argument are scoped some other way; nothing to enforce.
        if (!context.ActionArguments.TryGetValue(ParameterName, out var raw))
        {
            return;
        }

        var requested = raw switch
        {
            Guid g => g,
            string s when Guid.TryParse(s, out var parsed) => parsed,
            _ => Guid.Empty,
        };

        var logger = context.HttpContext.RequestServices
            .GetService<ILoggerFactory>()
            ?.CreateLogger(nameof(ValidateTenantScopeAttribute));

        var resolution = CrossTenant
            ? TenantAuthority.ResolveForCrossTenantRequest(context.HttpContext, requested, logger)
            : TenantAuthority.ResolveForTenantScopedRequest(context.HttpContext, requested, logger);

        if (!resolution.IsAuthorized)
        {
            var failure = ApiResponseFactory.Failure<object>(
                ApiResponseCode.FORBIDDEN,
                context.HttpContext.TraceIdentifier);

            context.Result = new ObjectResult(failure) { StatusCode = StatusCodes.Status403Forbidden };
            return;
        }

        // Authoritative value wins, whatever the caller sent.
        context.ActionArguments[ParameterName] = raw is Guid?
            ? resolution.OrganizationId
            : (object)resolution.OrganizationId;
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        // Nothing to do after the action; enforcement happens before it runs.
    }
}
