using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ComplianceService.Infrastructure.Filters;

/// <summary>
/// Maps client disconnection and cooperative cancellation (e.g. HttpContext.RequestAborted) to
/// 499 / Client Closed Request instead of 500, and marks the exception handled so it is not
/// treated as an application fault (EF may throw <see cref="OperationCanceledException" /> during
/// <c>FirstOrDefaultAsync</c> when the request is aborted mid-query).
/// </summary>
public sealed class IgnoreRequestAbortedExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not OperationCanceledException)
        {
            return;
        }

        // 499: non-standard but widely used for "client closed request" (Nginx, etc.)
        context.Result = new StatusCodeResult(499);
        context.ExceptionHandled = true;
    }
}
