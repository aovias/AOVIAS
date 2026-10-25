namespace ComplianceServiceManager.Services.Deployment;

/// <summary>
/// Bridges Release SDK HTTP tracing into the Command deployment trace log (Sprint 11.3).
/// </summary>
public sealed class ReleaseClientTraceBridge : Aovias.Release.SDK.IReleaseClientTraceHook
{
    private readonly IDeploymentRequestTraceService traceService;

    public ReleaseClientTraceBridge(IDeploymentRequestTraceService traceService)
    {
        this.traceService = traceService;
    }

    public void OnHttpDispatch(
        string operation,
        string url,
        string httpMethod,
        string? requestBody,
        IReadOnlyDictionary<string, string> headers)
    {
        traceService.TraceHttpSent(url, httpMethod, headers);
        if (!string.IsNullOrWhiteSpace(requestBody))
        {
            traceService.TraceStage($"{operation}.Payload", requestBody);
        }
    }

    public void OnHttpResponse(string operation, int statusCode, string? responseBody)
        => traceService.TraceHttpResponse(statusCode, responseBody);
}
