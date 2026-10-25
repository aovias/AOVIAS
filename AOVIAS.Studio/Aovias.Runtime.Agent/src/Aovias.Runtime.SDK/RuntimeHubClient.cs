using Aovias.Configuration.Settings;
using Aovias.Configuration.SignalR;
using Aovias.Runtime.Contracts.Deployment;
using Aovias.Runtime.Contracts.Observability;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;

namespace Aovias.Runtime.SDK;

/// <summary>
/// SignalR client for runtime workflow and health events.
/// </summary>
public sealed class RuntimeHubClient : IRuntimeHubClient, IAsyncDisposable
{
    private readonly IRuntimeAgentClient runtimeAgentClient;
    private readonly IOptionsMonitor<ApiSettings> apiSettings;
    private readonly IOptionsMonitor<SignalRSettings> signalRSettings;
    private HubConnection? connection;

    public RuntimeHubClient(
        IRuntimeAgentClient runtimeAgentClient,
        IOptionsMonitor<ApiSettings> apiSettings,
        IOptionsMonitor<SignalRSettings> signalRSettings)
    {
        this.runtimeAgentClient = runtimeAgentClient;
        this.apiSettings = apiSettings;
        this.signalRSettings = signalRSettings;
    }

    /// <inheritdoc />
    public RuntimeHubConnectionDiagnostics? LastConnectionDiagnostics { get; private set; }

    /// <inheritdoc />
    public event EventHandler<WorkflowProgressMessage>? WorkflowProgressReceived;

    /// <inheritdoc />
    public event EventHandler<WorkflowProgressMessage>? DeploymentProgressReceived;

    /// <inheritdoc />
    public event EventHandler<RuntimeHealthMessage>? RuntimeHealthReceived;

    /// <inheritdoc />
    public event EventHandler<WorkflowEventMessage>? WorkflowEventReceived;

    /// <inheritdoc />
    public event EventHandler<RuntimeStateDto>? RuntimeStateReceived;

    /// <inheritdoc />
    public event EventHandler<RuntimeTimelineEntryDto>? RuntimeTimelineReceived;

    /// <inheritdoc />
    public event EventHandler<RuntimeMetricsSnapshotDto>? RuntimeMetricsReceived;

    /// <inheritdoc />
    public event EventHandler<RuntimeTimelineEntryDto>? ContainerEventReceived;

    /// <inheritdoc />
    public event EventHandler<RuntimeTimelineEntryDto>? WorkflowUpdateReceived;

    /// <inheritdoc />
    public event EventHandler<RuntimeTimelineEntryDto>? DeploymentUpdateReceived;

    /// <inheritdoc />
    public event EventHandler<WorkflowProgressMessage>? RollbackProgressReceived;

    /// <inheritdoc />
    public event EventHandler<DeploymentPhaseEventDto>? DeploymentPhaseReceived;

    /// <inheritdoc />
    public event EventHandler<DeploymentOperatorErrorDto>? DeploymentFailureReceived;

    /// <inheritdoc />
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        var settings = signalRSettings.CurrentValue;
        var api = apiSettings.CurrentValue;
        var configuredHubPath = settings.HubPath;
        var configuredHubUrl = SignalRHubEndpointResolver.ResolveHubUrl(api.BaseUrl, api.PathBase, configuredHubPath);

        if (!settings.Enabled)
        {
            LastConnectionDiagnostics = RuntimeHubConnectionDiagnostics.Failure(
                expectedHubPath: string.Empty,
                configuredHubPath: configuredHubPath,
                attemptedHubUrl: configuredHubUrl,
                failureReason: "SignalR is disabled in client configuration.",
                discoverySucceeded: false);
            return;
        }

        if (connection is not null && connection.State == HubConnectionState.Connected)
        {
            return;
        }

        var discovery = await TryDiscoverHubAsync(cancellationToken);
        var expectedHubPath = discovery?.SignalR?.Hub
            ?? SignalRHubEndpointResolver.ResolveHubPath(api.PathBase, configuredHubPath);
        var candidateHubUrls = SignalRHubEndpointResolver.ResolveConnectionCandidateHubUrls(
            api.BaseUrl,
            api.PathBase,
            discovery?.SignalR?.Hub,
            configuredHubPath);
        var discoverySucceeded = discovery?.SignalR?.Enabled == true;

        Exception? lastException = null;
        foreach (var hubUrl in candidateHubUrls)
        {
            try
            {
                if (connection is not null)
                {
                    await connection.StopAsync(cancellationToken);
                    await connection.DisposeAsync();
                    connection = null;
                }

                connection = new HubConnectionBuilder()
                    .WithUrl(hubUrl, options =>
                    {
                        options.Transports = HttpTransportType.WebSockets | HttpTransportType.LongPolling;
                        options.SkipNegotiation = false;
                    })
                    .WithAutomaticReconnect()
                    .Build();

                RegisterHandlers(connection);
                await connection.StartAsync(cancellationToken);
                await connection.InvokeAsync("SubscribeAsync", cancellationToken);

                LastConnectionDiagnostics = RuntimeHubConnectionDiagnostics.Success(
                    expectedHubPath,
                    configuredHubPath,
                    hubUrl,
                    discoverySucceeded);
                return;
            }
            catch (Exception exception)
            {
                lastException = exception;
            }
        }

        var attemptedHubUrl = candidateHubUrls.Count > 0
            ? candidateHubUrls[^1]
            : configuredHubUrl;
        var failureReason = candidateHubUrls.Count > 1
            ? $"Tried {string.Join(", ", candidateHubUrls)}. Last error: {lastException?.Message}"
            : lastException?.Message ?? "SignalR connection failed.";
        LastConnectionDiagnostics = RuntimeHubConnectionDiagnostics.Failure(
            expectedHubPath,
            configuredHubPath,
            attemptedHubUrl,
            failureReason,
            discoverySucceeded);
        throw lastException ?? new InvalidOperationException("SignalR connection failed.");
    }

    /// <inheritdoc />
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (connection is null)
        {
            return;
        }

        await connection.StopAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (connection is not null)
        {
            await connection.DisposeAsync();
        }
    }

    private async Task<Runtime.Contracts.Deployment.RuntimeDeploymentAvailabilityDto?> TryDiscoverHubAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            return await runtimeAgentClient.GetDeploymentAvailabilityAsync(cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    private void RegisterHandlers(HubConnection hubConnection)
    {
        hubConnection.On<WorkflowProgressMessage>("WorkflowProgress", message => WorkflowProgressReceived?.Invoke(this, message));
        hubConnection.On<WorkflowProgressMessage>("DeploymentProgress", message => DeploymentProgressReceived?.Invoke(this, message));
        hubConnection.On<WorkflowProgressMessage>("RollbackProgress", message => RollbackProgressReceived?.Invoke(this, message));
        hubConnection.On<DeploymentPhaseEventDto>("DeploymentPhase", phase => DeploymentPhaseReceived?.Invoke(this, phase));
        hubConnection.On<DeploymentOperatorErrorDto>("DeploymentFailure", failure => DeploymentFailureReceived?.Invoke(this, failure));
        hubConnection.On<RuntimeHealthMessage>("RuntimeHealth", message => RuntimeHealthReceived?.Invoke(this, message));
        hubConnection.On<RuntimeStateDto>("RuntimeState", state => RuntimeStateReceived?.Invoke(this, state));
        hubConnection.On<RuntimeTimelineEntryDto>("RuntimeTimeline", entry => RuntimeTimelineReceived?.Invoke(this, entry));
        hubConnection.On<RuntimeMetricsSnapshotDto>("RuntimeMetrics", metrics => RuntimeMetricsReceived?.Invoke(this, metrics));
        hubConnection.On<RuntimeTimelineEntryDto>("ContainerEvent", entry => ContainerEventReceived?.Invoke(this, entry));
        hubConnection.On<RuntimeTimelineEntryDto>("WorkflowUpdate", entry => WorkflowUpdateReceived?.Invoke(this, entry));
        hubConnection.On<RuntimeTimelineEntryDto>("DeploymentUpdate", entry => DeploymentUpdateReceived?.Invoke(this, entry));
        hubConnection.On<WorkflowEventMessage>("WorkflowEvent", message =>
        {
            WorkflowEventReceived?.Invoke(this, message);
            WorkflowProgressReceived?.Invoke(this, new WorkflowProgressMessage
            {
                WorkflowId = message.WorkflowId,
                EventType = message.EventType,
                StepName = message.StepName,
                Message = message.Message,
                Timestamp = message.Timestamp
            });
        });
    }
}
