using Microsoft.AspNetCore.Mvc.Filters;

namespace ComplianceService.Infrastructure.Filters;

/// <summary>
/// Legacy attribute: hospital access is enforced globally by <see cref="HospitalAccessActionFilter"/>.
/// Kept so existing controller attributes continue to compile; execution is a no-op.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ValidateHospitalIdAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);
    }
}
