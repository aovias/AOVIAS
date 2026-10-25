using System.Net.Http.Json;
using Aovias.Configuration.Settings;
using Aovias.Release.Contracts.Deployment;
using Aovias.Release.Contracts.RemoteDeployment;
using Aovias.Release.Contracts.Releases;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aovias.Release.SDK;

/// <summary>
/// HTTP client for the Remote Deployment Orchestrator API on the Runtime Agent.
/// </summary>
public sealed class DeploymentClient : IDeploymentClient
{
    private readonly HttpClient httpClient;
    private readonly IOptionsMonitor<RemoteDeploymentSettings> deploymentSettings;
    private readonly ILogger<DeploymentClient>? logger;

    public DeploymentClient(
        HttpClient httpClient,
        IOptionsMonitor<RemoteDeploymentSettings> deploymentSettings,
        ILogger<DeploymentClient>? logger = null)
    {
        this.httpClient = httpClient;
        this.deploymentSettings = deploymentSettings;
        this.logger = logger;
    }

    public Task<PlatformDeploymentResponse> DeployPlatformAsync(
        PlatformDeploymentRequest request,
        CancellationToken cancellationToken = default)
    {
        LogBoundaryEnter("DeployPlatformAsync", deploymentSettings.CurrentValue.PlatformEndpoint);
        return SendAsync<PlatformDeploymentResponse>(
            "DeployPlatformAsync",
            deploymentSettings.CurrentValue.PlatformEndpoint,
            () => httpClient.PostAsJsonAsync(RequestUri(deploymentSettings.CurrentValue.PlatformEndpoint), request, cancellationToken),
            "Deploy",
            cancellationToken);
    }

    public async Task<PlatformDeploymentResponse> UploadAndDeployPackageAsync(
        string packagePath,
        string runtime,
        string environment,
        string uploadedBy,
        bool operatorConfirmed = true,
        CancellationToken cancellationToken = default)
    {
        var fileInfo = new FileInfo(packagePath);
        if (fileInfo.Length > deploymentSettings.CurrentValue.UploadChunkSizeBytes)
        {
            throw new DeploymentFailureException(new DeploymentFailure
            {
                Category = DeploymentFailureCategory.Upload.ToString(),
                Phase = "Preflight",
                Component = "DeploymentClient",
                Operation = "UploadAndDeployPackageAsync",
                ExceptionType = nameof(InvalidOperationException),
                Message =
                    $"Single-request upload rejected before network transfer: '{fileInfo.Name}' is {fileInfo.Length} bytes.",
                Recovery = "Use the resumable chunked deployment pipeline for packages larger than the configured chunk size.",
                IsTransient = false
            });
        }

        // Buffer the package in memory so HttpClient does not stream-copy a FileStream through
        // multipart. This compatibility endpoint is restricted to one configured chunk.
        var packageBytes = await File.ReadAllBytesAsync(packagePath, cancellationToken);
        using var content = new MultipartFormDataContent
        {
            { new ByteArrayContent(packageBytes), "package", Path.GetFileName(packagePath) },
            { new StringContent(runtime), "runtime" },
            { new StringContent(environment), "environment" },
            { new StringContent(uploadedBy), "uploadedBy" },
            { new StringContent(operatorConfirmed.ToString()), "operatorConfirmed" }
        };

        LogBoundaryEnter("UploadAndDeployPackageAsync", deploymentSettings.CurrentValue.DeployEndpoint);
        return await SendAsync<PlatformDeploymentResponse>(
            "UploadAndDeployPackageAsync",
            deploymentSettings.CurrentValue.DeployEndpoint,
            () => httpClient.PostAsync(RequestUri(deploymentSettings.CurrentValue.DeployEndpoint), content, cancellationToken),
            "Upload",
            cancellationToken);
    }

    public Task<DeploymentStatusResponse?> GetStatusAsync(string deploymentId, CancellationToken cancellationToken = default)
    {
        var endpoint = deploymentSettings.CurrentValue.StatusEndpointTemplate
            .Replace("{deploymentId}", deploymentId, StringComparison.OrdinalIgnoreCase);

        return SendOptionalAsync<DeploymentStatusResponse>(
            "GetStatusAsync",
            endpoint,
            () => httpClient.GetAsync(RequestUri(endpoint), cancellationToken),
            "Monitor",
            cancellationToken);
    }

    public Task<IReadOnlyList<string>> GetLogsAsync(
        string deploymentId,
        string? serviceId = null,
        CancellationToken cancellationToken = default)
    {
        var endpoint = deploymentSettings.CurrentValue.LogsEndpointTemplate
            .Replace("{deploymentId}", deploymentId, StringComparison.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(serviceId))
        {
            endpoint += $"?serviceId={Uri.EscapeDataString(serviceId)}";
        }

        return SendAsync<IReadOnlyList<string>>(
            "GetLogsAsync",
            endpoint,
            () => httpClient.GetAsync(RequestUri(endpoint), cancellationToken),
            "Monitor",
            cancellationToken);
    }

    public Task<IReadOnlyList<PlatformDeploymentHistoryEntry>> GetHistoryAsync(CancellationToken cancellationToken = default)
        => SendAsync<IReadOnlyList<PlatformDeploymentHistoryEntry>>(
            "GetHistoryAsync",
            deploymentSettings.CurrentValue.HistoryEndpoint,
            () => httpClient.GetAsync(RequestUri(deploymentSettings.CurrentValue.HistoryEndpoint), cancellationToken),
            "History",
            cancellationToken);

    public Task<PlatformDeploymentReport?> GetReportAsync(string deploymentId, CancellationToken cancellationToken = default)
    {
        var endpoint = deploymentSettings.CurrentValue.ReportEndpointTemplate
            .Replace("{deploymentId}", deploymentId, StringComparison.OrdinalIgnoreCase);

        return SendOptionalAsync<PlatformDeploymentReport>(
            "GetReportAsync",
            endpoint,
            () => httpClient.GetAsync(RequestUri(endpoint), cancellationToken),
            "Verify",
            cancellationToken);
    }

    public async Task<bool> CancelAsync(CancelDeploymentRequest request, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<CancelResponse>(
            "CancelAsync",
            deploymentSettings.CurrentValue.CancelEndpoint,
            () => httpClient.PostAsJsonAsync(RequestUri(deploymentSettings.CurrentValue.CancelEndpoint), request, cancellationToken),
            "Cancel",
            cancellationToken);

        return response.Cancelled;
    }

    public Task<PlatformDeploymentResponse> RollbackAsync(
        PlatformRollbackRequest request,
        CancellationToken cancellationToken = default)
        => SendAsync<PlatformDeploymentResponse>(
            "RollbackAsync",
            deploymentSettings.CurrentValue.RollbackEndpoint,
            () => httpClient.PostAsJsonAsync(RequestUri(deploymentSettings.CurrentValue.RollbackEndpoint), request, cancellationToken),
            "Rollback",
            cancellationToken);

    public Task<AgentRegistrationResponse> RegisterEnvironmentAsync(
        AgentRegistrationRequest request,
        CancellationToken cancellationToken = default)
        => SendAsync<AgentRegistrationResponse>(
            "RegisterEnvironmentAsync",
            deploymentSettings.CurrentValue.RegisterEndpoint,
            () => httpClient.PostAsJsonAsync(RequestUri(deploymentSettings.CurrentValue.RegisterEndpoint), request, cancellationToken),
            "Register",
            cancellationToken);

    public Task<EnvironmentHealthSnapshot?> GetEnvironmentHealthAsync(
        string environmentId,
        CancellationToken cancellationToken = default)
    {
        var endpoint = deploymentSettings.CurrentValue.HealthEndpointTemplate
            .Replace("{environmentId}", environmentId, StringComparison.OrdinalIgnoreCase);

        return SendOptionalAsync<EnvironmentHealthSnapshot>(
            "GetEnvironmentHealthAsync",
            endpoint,
            () => httpClient.GetAsync(RequestUri(endpoint), cancellationToken),
            "Health",
            cancellationToken);
    }

    public Task<EnvironmentDriftReport> DetectDriftAsync(
        string environmentId,
        CancellationToken cancellationToken = default)
    {
        var endpoint = deploymentSettings.CurrentValue.DriftEndpointTemplate
            .Replace("{environmentId}", environmentId, StringComparison.OrdinalIgnoreCase);

        return SendAsync<EnvironmentDriftReport>(
            "DetectDriftAsync",
            endpoint,
            () => httpClient.GetAsync(RequestUri(endpoint), cancellationToken),
            "Drift",
            cancellationToken);
    }

    public Task<DeploymentReliabilityMetrics> GetReliabilityMetricsAsync(CancellationToken cancellationToken = default)
        => SendAsync<DeploymentReliabilityMetrics>(
            "GetReliabilityMetricsAsync",
            "/api/deployment/metrics",
            () => httpClient.GetAsync(RequestUri("/api/deployment/metrics"), cancellationToken),
            "Metrics",
            cancellationToken);

    public Task<ChunkedUploadSession> InitiateChunkedUploadAsync(
        InitiateChunkedUploadRequest request,
        CancellationToken cancellationToken = default)
        => SendAsync<ChunkedUploadSession>(
            "InitiateChunkedUploadAsync",
            "/api/deployment/upload/initiate",
            async () =>
            {
                using var message = new HttpRequestMessage(HttpMethod.Post, RequestUri("/api/deployment/upload/initiate"))
                {
                    Content = JsonContent.Create(request)
                };
                message.Headers.Add("X-Correlation-Id", request.DeploymentId);
                logger?.LogInformation(
                    "Upload request created DeploymentId={DeploymentId} Stage=Initiate Package={Package} SizeBytes={SizeBytes}",
                    request.DeploymentId,
                    request.FileName,
                    request.TotalBytes);
                return await httpClient.SendAsync(message, cancellationToken);
            },
            "Upload",
            cancellationToken,
            request.DeploymentId);

    public async Task<ChunkedUploadSession> UploadChunkAsync(
        string uploadId,
        int chunkIndex,
        UploadChunkMetadata metadata,
        Stream chunkStream,
        CancellationToken cancellationToken = default)
    {
        await using var buffer = new MemoryStream();
        await chunkStream.CopyToAsync(buffer, cancellationToken);
        using var content = new ByteArrayContent(buffer.ToArray());
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        content.Headers.Add("X-Chunk-SHA256", metadata.Checksum);
        var endpoint = $"/api/deployment/upload/{uploadId}/chunk/{chunkIndex}";
        return await SendAsync<ChunkedUploadSession>(
            "UploadChunkAsync",
            endpoint,
            async () =>
            {
                using var message = new HttpRequestMessage(HttpMethod.Post, RequestUri(endpoint)) { Content = content };
                message.Headers.Add("X-Correlation-Id", metadata.DeploymentId);
                logger?.LogInformation(
                    "Upload headers written DeploymentId={DeploymentId} UploadId={UploadId} Chunk={Chunk} ContentLength={ContentLength} Checksum={Checksum}",
                    metadata.DeploymentId,
                    uploadId,
                    chunkIndex,
                    metadata.ContentLength,
                    metadata.Checksum);
                return await httpClient.SendAsync(message, cancellationToken);
            },
            "Upload",
            cancellationToken,
            metadata.DeploymentId);
    }

    public Task<ChunkedUploadSession> GetUploadSessionAsync(
        string uploadId,
        CancellationToken cancellationToken = default)
        => SendAsync<ChunkedUploadSession>(
            "GetUploadSessionAsync",
            $"/api/deployment/upload/{uploadId}",
            () => httpClient.GetAsync(RequestUri($"/api/deployment/upload/{uploadId}"), cancellationToken),
            "Upload",
            cancellationToken);

    public async Task<UploadPackageResult> CompleteChunkedUploadAsync(
        string uploadId,
        string runtime,
        string environment,
        string uploadedBy,
        CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent
        {
            { new StringContent(runtime), "runtime" },
            { new StringContent(environment), "environment" },
            { new StringContent(uploadedBy), "uploadedBy" }
        };

        var endpoint = $"/api/deployment/upload/{uploadId}/complete";
        return await SendAsync<UploadPackageResult>(
            "CompleteChunkedUploadAsync",
            endpoint,
            () => httpClient.PostAsync(RequestUri(endpoint), content, cancellationToken),
            "Upload",
            cancellationToken);
    }

    public Task<RuntimeHeartbeatDto> GetHeartbeatAsync(CancellationToken cancellationToken = default)
        => SendAsync<RuntimeHeartbeatDto>(
            "GetHeartbeatAsync",
            deploymentSettings.CurrentValue.HeartbeatEndpoint,
            () => httpClient.GetAsync(RequestUri(deploymentSettings.CurrentValue.HeartbeatEndpoint), cancellationToken),
            "Heartbeat",
            cancellationToken);

    public Task<PlatformReconciliationReport> ReconcileAsync(CancellationToken cancellationToken = default)
        => SendAsync<PlatformReconciliationReport>(
            "ReconcileAsync",
            deploymentSettings.CurrentValue.ReconcileEndpoint,
            () => httpClient.PostAsync(RequestUri(deploymentSettings.CurrentValue.ReconcileEndpoint), content: null, cancellationToken),
            "Reconcile",
            cancellationToken);

    public Task<MaintenanceModeStatus> GetMaintenanceModeAsync(CancellationToken cancellationToken = default)
        => SendAsync<MaintenanceModeStatus>(
            "GetMaintenanceModeAsync",
            deploymentSettings.CurrentValue.MaintenanceEndpoint,
            () => httpClient.GetAsync(RequestUri(deploymentSettings.CurrentValue.MaintenanceEndpoint), cancellationToken),
            "Maintenance",
            cancellationToken);

    public Task<MaintenanceModeStatus> SetMaintenanceModeAsync(MaintenanceModeRequest request, CancellationToken cancellationToken = default)
        => SendAsync<MaintenanceModeStatus>(
            "SetMaintenanceModeAsync",
            deploymentSettings.CurrentValue.MaintenanceEndpoint,
            () => httpClient.PostAsJsonAsync(RequestUri(deploymentSettings.CurrentValue.MaintenanceEndpoint), request, cancellationToken),
            "Maintenance",
            cancellationToken);

    public Task<MaintenanceModeStatus> ResumeFromMaintenanceAsync(string resumedBy, CancellationToken cancellationToken = default)
        => SendAsync<MaintenanceModeStatus>(
            "ResumeFromMaintenanceAsync",
            deploymentSettings.CurrentValue.MaintenanceResumeEndpoint,
            () => httpClient.PostAsJsonAsync(RequestUri(deploymentSettings.CurrentValue.MaintenanceResumeEndpoint), new MaintenanceModeRequest { RequestedBy = resumedBy }, cancellationToken),
            "Maintenance",
            cancellationToken);

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

    private void LogBoundaryEnter(string operation, string endpoint)
        => logger?.LogInformation(
            "DeploymentClient entering {Operation} endpoint={Endpoint}",
            operation,
            endpoint);

    private async Task<T> SendAsync<T>(
        string operation,
        string endpoint,
        Func<Task<HttpResponseMessage>> send,
        string phase,
        CancellationToken cancellationToken,
        string? correlationId = null)
    {
        HttpResponseMessage response;
        try
        {
            response = await send();
        }
        catch (DeploymentFailureException)
        {
            throw;
        }
        catch (Exception exception)
        {
            var failure = DeploymentFailureMapper.FromException(
                exception,
                phase,
                "DeploymentClient",
                operation,
                DeploymentFailureCategory.Upload,
                correlationId: correlationId);
            logger?.LogError(
                exception,
                "Deployment transport failure Operation={Operation} Endpoint={Endpoint} Transient={Transient} ExceptionChain={ExceptionChain}",
                operation,
                endpoint,
                failure.IsTransient,
                string.Join(" -> ", failure.ExceptionChain.Select(item =>
                    $"{item.Type}: {item.Message} [HResult={item.HResult}, Socket={item.SocketError}]")));
            throw new DeploymentFailureException(failure);
        }

        using (response)
        {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw DeploymentHttpFailureHandler.CreateFailure(
                operation,
                endpoint,
                (int)response.StatusCode,
                body,
                phase,
                "DeploymentClient",
                serviceId: null,
                correlationId: null,
                logger);
        }

        logger?.LogInformation(
            "DeploymentClient exiting {Operation} endpoint={Endpoint} status={StatusCode}",
            operation,
            endpoint,
            (int)response.StatusCode);

        var payload = System.Text.Json.JsonSerializer.Deserialize<T>(body, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        return payload ?? throw new InvalidOperationException($"Deployment API call to '{endpoint}' returned an empty payload.");
        }
    }

    private async Task<T?> SendOptionalAsync<T>(
        string operation,
        string endpoint,
        Func<Task<HttpResponseMessage>> send,
        string phase,
        CancellationToken cancellationToken)
        where T : class
    {
        var response = await send();
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw DeploymentHttpFailureHandler.CreateFailure(
                operation,
                endpoint,
                (int)response.StatusCode,
                body,
                phase,
                "DeploymentClient",
                serviceId: null,
                correlationId: null,
                logger);
        }

        return System.Text.Json.JsonSerializer.Deserialize<T>(body, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    private sealed class CancelResponse
    {
        public bool Cancelled { get; init; }
    }
}
