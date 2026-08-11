using System.Text.Json;
using Aovias.Configuration.Settings;
using ComplianceServiceManager.Logging;
using Microsoft.Extensions.Options;

namespace ComplianceServiceManager.Services.Deployment;

/// <summary>
/// Traces deployment HTTP requests from AOVIAS Command to the Runtime Agent (Sprint 11.3).
/// </summary>
public interface IDeploymentRequestTraceService
{
    void TraceStage(string stage, string message, object? data = null);

    void TraceDeployButtonClicked(
        string? packagePath,
        string? releaseId,
        string? serviceName);

    void TraceRequestBuilt(
        string httpMethod,
        string endpoint,
        string requestModelType,
        object requestPayload,
        string? packagePath);

    void TraceHttpSent(string url, string httpMethod, IReadOnlyDictionary<string, string>? headers = null);

    void TraceHttpResponse(int statusCode, string? responseBody);

    void TraceEndpointComparison();
}

public sealed class DeploymentRequestTraceService : IDeploymentRequestTraceService
{
    private readonly ILoggingService loggingService;
    private readonly IOptionsMonitor<ApiSettings> apiSettings;
    private readonly IOptionsMonitor<ReleaseManagementSettings> releaseSettings;
    private readonly IOptionsMonitor<RemoteDeploymentSettings> remoteDeploymentSettings;

    public DeploymentRequestTraceService(
        ILoggingService loggingService,
        IOptionsMonitor<ApiSettings> apiSettings,
        IOptionsMonitor<ReleaseManagementSettings> releaseSettings,
        IOptionsMonitor<RemoteDeploymentSettings> remoteDeploymentSettings)
    {
        this.loggingService = loggingService;
        this.apiSettings = apiSettings;
        this.releaseSettings = releaseSettings;
        this.remoteDeploymentSettings = remoteDeploymentSettings;
    }

    public void TraceStage(string stage, string message, object? data = null)
    {
        var payload = data is null ? message : $"{message} | {SerializeSafe(data)}";
        loggingService.Process("DeploymentTrace", $"[{stage}] {payload}");
        Serilog.Log.Information("DeploymentTrace [{Stage}] {Message} {@Data}", stage, message, data);
    }

    public void TraceDeployButtonClicked(string? packagePath, string? releaseId, string? serviceName)
    {
        TraceStage("DeployButtonClicked", "Operator initiated deploy.", new
        {
            PackagePath = packagePath,
            ReleaseId = releaseId,
            ServiceName = serviceName
        });
    }

    public void TraceRequestBuilt(
        string httpMethod,
        string endpoint,
        string requestModelType,
        object requestPayload,
        string? packagePath)
    {
        TraceStage("RequestBuilt", "Deployment request assembled.", new
        {
            HttpMethod = httpMethod,
            Endpoint = endpoint,
            RequestModelType = requestModelType,
            SerializedPayload = SerializeSafe(requestPayload),
            PackagePath = packagePath,
            RuntimeUrl = apiSettings.CurrentValue.BaseUrl
        });
    }

    public void TraceHttpSent(string url, string httpMethod, IReadOnlyDictionary<string, string>? headers = null)
    {
        TraceStage("HttpPostSent", "HTTP request dispatched.", new
        {
            Url = url,
            HttpMethod = httpMethod,
            Headers = headers ?? new Dictionary<string, string>()
        });
    }

    public void TraceHttpResponse(int statusCode, string? responseBody)
    {
        TraceStage("HttpResponseReceived", "HTTP response received.", new
        {
            StatusCode = statusCode,
            ResponseBody = responseBody
        });
    }

    public void TraceEndpointComparison()
    {
        var release = releaseSettings.CurrentValue;
        var remote = remoteDeploymentSettings.CurrentValue;
        TraceStage("EndpointComparison", "Single-service deploy uses Release API, not DeploymentController multipart deploy.", new
        {
            RuntimeBaseUrl = apiSettings.CurrentValue.BaseUrl,
            SingleServiceDeploy = new
            {
                ClientEndpoint = release.DeployEndpoint,
                HttpMethod = "POST",
                RequestDto = nameof(Aovias.Release.Contracts.Releases.DeployReleaseRequest),
                Controller = "ReleasesController.DeployAsync"
            },
            PackageMultipartDeploy = new
            {
                ClientEndpoint = remote.DeployEndpoint,
                HttpMethod = "POST",
                RequestDto = "multipart/form-data with package file",
                Controller = "DeploymentController.DeployAsync"
            }
        });
    }

    private static string SerializeSafe(object value)
    {
        try
        {
            return JsonSerializer.Serialize(value);
        }
        catch
        {
            return value.ToString() ?? string.Empty;
        }
    }
}
