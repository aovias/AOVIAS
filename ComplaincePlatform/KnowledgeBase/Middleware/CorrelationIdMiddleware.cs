namespace KnowledgeBase.Api.Middleware;

public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    public const string HeaderName = "X-Correlation-ID";

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var cid = context.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(cid))
            cid = Guid.NewGuid().ToString("N");

        context.Response.Headers[HeaderName] = cid;
        await _next(context);
    }
}
