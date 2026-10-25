namespace Gateway.Middleware;

/// <summary>
/// Enables request body buffering for multipart POSTs as early as possible so later components
/// (auth, model binding, filters, proxy) cannot drain <see cref="Microsoft.AspNetCore.Http.HttpRequest.Body"/>
/// before <see cref="Gateway.Application.Http.MultipartProxyRequestBody"/> copies it. Without this,
/// <c>CopyToAsync</c> can read 0 bytes while <c>Content-Length</c> is non-zero.
/// </summary>
public sealed class MultipartRequestBufferingMiddleware
{
    private readonly RequestDelegate _next;

    public MultipartRequestBufferingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsPost(context.Request.Method))
        {
            var ct = context.Request.ContentType;
            if (!string.IsNullOrEmpty(ct) &&
                ct.Contains("multipart/form-data", StringComparison.OrdinalIgnoreCase))
            {
                context.Request.EnableBuffering();
            }
        }

        await _next(context);
    }
}
