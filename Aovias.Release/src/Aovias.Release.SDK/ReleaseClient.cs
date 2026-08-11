using System.Net.Http.Json;
using System.Text.Json;
using Aovias.Configuration.Settings;
using Aovias.Release.Contracts.Deployment;
using Aovias.Release.Contracts.Packages;
using Aovias.Release.Contracts.RemoteDeployment;
using Aovias.Release.Contracts.Releases;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aovias.Release.SDK;

/// <summary>
/// Optional deployment HTTP tracing hook used by AOVIAS Command (Sprint 11.3).
/// </summary>
public interface IReleaseClientTraceHook
{
    void OnHttpDispatch(string operation, string url, string httpMethod, string? requestBody, IReadOnlyDictionary<string, string> headers);

    void OnHttpResponse(string operation, int statusCode, string? responseBody);
}

/// <summary>
/// HTTP client for the AOVIAS Release Management Platform API.
/// </summary>
public sealed class ReleaseClient : IReleaseClient
{
    private readonly HttpClient httpClient;
    private readonly IOptionsMonitor<ReleaseManagementSettings> releaseSettings;
    private readonly ILogger<ReleaseClient>? logger;
    private readonly IReleaseClientTraceHook? traceHook;

    public ReleaseClient(
        HttpClient httpClient,
        IOptionsMonitor<ReleaseManagementSettings> releaseSettings,
        ILogger<ReleaseClient>? logger = null,
        IReleaseClientTraceHook? traceHook = null)
    {
        this.httpClient = httpClient;
        this.releaseSettings = releaseSettings;
        this.logger = logger;
        this.traceHook = traceHook;
    }

    /// <inheritdoc />
    public async Task<UploadPackageResult> UploadPackageAsync(
        string packagePath,
        string runtime,
        string environment,
        string uploadedBy,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(packagePath))
        {
            throw new FileNotFoundException(
                $"Upload rejected before network transfer: package '{packagePath}' does not exist.",
                packagePath);
        }

        TraceDispatch("UploadPackage", releaseSettings.CurrentValue.UploadEndpoint, "POST", new
        {
            packagePath,
            runtime,
            environment,
            uploadedBy
        });

        return await SendAsync<UploadPackageResult>(
            "UploadPackage",
            releaseSettings.CurrentValue.UploadEndpoint,
            async () =>
            {
                // Reopen the file and rebuild multipart content for every transient attempt.
                // Retrying a consumed StreamContent caused "Error while copying content to a stream."
                await using var stream = new FileStream(
                    packagePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 1024 * 1024,
                    useAsync: true);
                using var content = new MultipartFormDataContent
                {
                    { new StreamContent(stream), "package", Path.GetFileName(packagePath) },
                    { new StringContent(runtime), "runtime" },
                    { new StringContent(environment), "environment" },
                    { new StringContent(uploadedBy), "uploadedBy" }
                };
                return await httpClient.PostAsync(
                    RequestUri(releaseSettings.CurrentValue.UploadEndpoint),
                    content,
                    cancellationToken);
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<PackageValidationResult> ValidatePackageAsync(string releaseId, CancellationToken cancellationToken = default)
    {
        var payload = new ValidateReleaseRequest { ReleaseId = releaseId };
        TraceDispatch("ValidateRelease", releaseSettings.CurrentValue.ValidateEndpoint, "POST", payload);
        return SendAsync<PackageValidationResult>(
            "ValidateRelease",
            releaseSettings.CurrentValue.ValidateEndpoint,
            () => httpClient.PostAsJsonAsync(
                RequestUri(releaseSettings.CurrentValue.ValidateEndpoint),
                payload,
                cancellationToken),
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<DeploymentPlan> GenerateDeploymentPlanAsync(
        string releaseId,
        string requestedBy,
        CancellationToken cancellationToken = default)
    {
        var payload = new PlanDeploymentRequest { ReleaseId = releaseId, RequestedBy = requestedBy };
        TraceDispatch("PlanDeployment", releaseSettings.CurrentValue.PlanEndpoint, "POST", payload);
        return SendAsync<DeploymentPlan>(
            "PlanDeployment",
            releaseSettings.CurrentValue.PlanEndpoint,
            () => httpClient.PostAsJsonAsync(
                RequestUri(releaseSettings.CurrentValue.PlanEndpoint),
                payload,
                cancellationToken),
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<DeployReleaseResult> DeployAsync(
        string releaseId,
        string requestedBy,
        bool operatorConfirmed,
        CancellationToken cancellationToken = default)
    {
        var payload = new DeployReleaseRequest
        {
            ReleaseId = releaseId,
            RequestedBy = requestedBy,
            OperatorConfirmed = operatorConfirmed
        };

        TraceDispatch("DeployRelease", releaseSettings.CurrentValue.DeployEndpoint, "POST", payload);

        return SendAsync<DeployReleaseResult>(
            "DeployRelease",
            releaseSettings.CurrentValue.DeployEndpoint,
            () => httpClient.PostAsJsonAsync(
                RequestUri(releaseSettings.CurrentValue.DeployEndpoint),
                payload,
                cancellationToken),
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<RollbackReleaseResult> RollbackAsync(
        string releaseId,
        string requestedBy,
        CancellationToken cancellationToken = default)
        => SendAsync<RollbackReleaseResult>(
            "RollbackRelease",
            releaseSettings.CurrentValue.RollbackEndpoint,
            () => httpClient.PostAsJsonAsync(
                RequestUri(releaseSettings.CurrentValue.RollbackEndpoint),
                new RollbackReleaseRequest { ReleaseId = releaseId, RequestedBy = requestedBy },
                cancellationToken),
            cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<ReleaseListItem>> GetReleasesAsync(CancellationToken cancellationToken = default)
        => SendAsync<IReadOnlyList<ReleaseListItem>>(
            "GetReleases",
            releaseSettings.CurrentValue.ListEndpoint,
            () => httpClient.GetAsync(RequestUri(releaseSettings.CurrentValue.ListEndpoint), cancellationToken),
            cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<ReleaseHistoryEntry>> GetHistoryAsync(CancellationToken cancellationToken = default)
        => SendAsync<IReadOnlyList<ReleaseHistoryEntry>>(
            "GetHistory",
            releaseSettings.CurrentValue.HistoryEndpoint,
            () => httpClient.GetAsync(RequestUri(releaseSettings.CurrentValue.HistoryEndpoint), cancellationToken),
            cancellationToken);

    /// <inheritdoc />
    public Task<ReleaseRecord?> GetReleaseAsync(string releaseId, CancellationToken cancellationToken = default)
    {
        var endpoint = releaseSettings.CurrentValue.DetailEndpointTemplate.Replace("{id}", releaseId, StringComparison.OrdinalIgnoreCase);
        return SendAsync<ReleaseRecord?>(
            "GetRelease",
            endpoint,
            () => httpClient.GetAsync(RequestUri(endpoint), cancellationToken),
            cancellationToken);
    }

    private async Task<T> SendAsync<T>(
        string operation,
        string requestUri,
        Func<Task<HttpResponseMessage>> send,
        CancellationToken cancellationToken)
    {
        var settings = releaseSettings.CurrentValue;
        var attempts = Math.Max(1, settings.RetryCount + 1);
        Exception? lastException = null;
        var absoluteUrl = BuildAbsoluteUrl(requestUri);

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                logger?.LogInformation(
                    "ReleaseClient sending {Operation} attempt={Attempt} url={Url}",
                    operation,
                    attempt,
                    absoluteUrl);

                using var response = await send();
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                traceHook?.OnHttpResponse(operation, (int)response.StatusCode, body);

                logger?.LogInformation(
                    "ReleaseClient received {Operation} status={StatusCode} body={Body}",
                    operation,
                    (int)response.StatusCode,
                    body);

                if (!response.IsSuccessStatusCode)
                {
                    throw DeploymentHttpFailureHandler.CreateFailure(
                        operation,
                        absoluteUrl,
                        (int)response.StatusCode,
                        body,
                        ResolvePhase(operation),
                        "ReleaseClient",
                        serviceId: null,
                        correlationId: null,
                        logger);
                }

                var payload = JsonSerializer.Deserialize<T>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return payload!;
            }
            catch (DeploymentFailureException)
            {
                throw;
            }
            catch (Exception exception) when (
                attempt < attempts
                && !cancellationToken.IsCancellationRequested
                && DeploymentFailureMapper.IsTransient(exception))
            {
                lastException = exception;
                await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt), cancellationToken);
            }
            catch (Exception exception)
            {
                lastException = exception;
                break;
            }
        }

        if (lastException is not null)
        {
            throw new DeploymentFailureException(
                DeploymentFailureMapper.FromException(
                    lastException,
                    ResolvePhase(operation),
                    "ReleaseClient",
                    operation,
                    operation == "UploadPackage"
                        ? DeploymentFailureCategory.Upload
                        : DeploymentFailureCategory.Application));
        }

        throw new InvalidOperationException($"Failed to call {requestUri}.");
    }

    private void TraceDispatch(string operation, string endpoint, string httpMethod, object payload)
    {
        var absoluteUrl = BuildAbsoluteUrl(endpoint);
        var headers = httpClient.DefaultRequestHeaders
            .Where(header => !string.Equals(header.Key, "X-Api-Key", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(header => header.Key, header => string.Join(", ", header.Value));

        logger?.LogInformation(
            "ReleaseClient dispatch {Operation} method={Method} url={Url} payload={Payload}",
            operation,
            httpMethod,
            absoluteUrl,
            SerializeSafe(payload));

        traceHook?.OnHttpDispatch(operation, absoluteUrl, httpMethod, SerializeSafe(payload), headers);
    }

    private string BuildAbsoluteUrl(string endpoint)
    {
        if (httpClient.BaseAddress is null)
        {
            return endpoint;
        }

        return new Uri(httpClient.BaseAddress, RequestUri(endpoint)).ToString();
    }

    private string RequestUri(string endpoint)
    {
        if (Uri.TryCreate(endpoint, UriKind.Absolute, out _)
            || httpClient.BaseAddress is null)
        {
            return endpoint;
        }

        var pathBase = httpClient.BaseAddress.AbsolutePath.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(pathBase) || pathBase == "/")
        {
            return endpoint;
        }

        if (endpoint.StartsWith($"{pathBase}/", StringComparison.OrdinalIgnoreCase))
        {
            return endpoint;
        }

        return $"{pathBase}/{endpoint.TrimStart('/')}";
    }

    private static string ResolvePhase(string operation)
        => operation switch
        {
            "DeployRelease" => "Deploy",
            "UploadPackage" => "Upload",
            "ValidateRelease" => "Analyze",
            "PlanDeployment" => "Analyze",
            _ => "Request"
        };

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
