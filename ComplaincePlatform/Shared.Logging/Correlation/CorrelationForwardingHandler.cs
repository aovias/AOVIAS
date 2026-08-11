using Microsoft.AspNetCore.Http;

namespace Shared.Logging.Correlation;

/// <summary>
/// DelegatingHandler that propagates the current request's correlation id to
/// downstream services so a single id flows across Gateway -> Compliance -> AI, etc.
/// Register on every outbound <c>HttpClient</c> via <c>.AddHttpMessageHandler&lt;CorrelationForwardingHandler&gt;()</c>.
/// </summary>
public sealed class CorrelationForwardingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationForwardingHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var correlationId = CorrelationContext.GetCorrelationId(_httpContextAccessor.HttpContext);
        if (!string.IsNullOrWhiteSpace(correlationId)
            && !request.Headers.Contains(LoggingConstants.CorrelationIdHeader))
        {
            request.Headers.TryAddWithoutValidation(LoggingConstants.CorrelationIdHeader, correlationId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
