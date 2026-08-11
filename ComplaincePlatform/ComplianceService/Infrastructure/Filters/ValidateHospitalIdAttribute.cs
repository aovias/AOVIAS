using ComplianceService.Application.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Reflection;

namespace ComplianceService.Infrastructure.Filters;

/// <summary>
/// Action filter that validates HospitalId in request body matches the hospital_id from JWT token.
/// Returns 403 Forbidden if there's a mismatch.
/// </summary>
public class ValidateHospitalIdAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var userContext = context.HttpContext.RequestServices.GetService<IUserContextService>();
        if (userContext == null)
        {
            // Log warning but don't fail - some endpoints might not need hospital validation
            // Return 500 only if this is critical
            context.Result = new StatusCodeResult(500);
            return;
        }

        var tokenHospitalId = userContext.HospitalId;

        // Check route parameters for hospitalId (case-insensitive)
        if (context.RouteData.Values.TryGetValue("hospitalId", out var routeHospitalIdValue))
        {
            if (routeHospitalIdValue != null && Guid.TryParse(routeHospitalIdValue.ToString(), out var routeHospitalId))
            {
                if (routeHospitalId != Guid.Empty && routeHospitalId != tokenHospitalId)
                {
                    context.Result = new ForbidResult();
                    return;
                }
            }
        }

        // Check all action parameters for HospitalId property
        foreach (var parameter in context.ActionDescriptor.Parameters)
        {
            var argument = context.ActionArguments.ContainsKey(parameter.Name) 
                ? context.ActionArguments[parameter.Name] 
                : null;

            if (argument == null) continue;

            // Check if the argument has a HospitalId property
            var hospitalIdProperty = argument.GetType().GetProperty("HospitalId", BindingFlags.Public | BindingFlags.Instance);
            if (hospitalIdProperty != null && hospitalIdProperty.PropertyType == typeof(Guid))
            {
                var requestHospitalId = (Guid)hospitalIdProperty.GetValue(argument)!;
                
                if (requestHospitalId != Guid.Empty && requestHospitalId != tokenHospitalId)
                {
                    context.Result = new ForbidResult();
                    return;
                }
            }
        }

        base.OnActionExecuting(context);
    }
}
