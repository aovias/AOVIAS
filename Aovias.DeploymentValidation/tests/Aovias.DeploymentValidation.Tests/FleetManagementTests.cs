using Aovias.CommandCenter.Infrastructure;
using Aovias.Release.Contracts.Releases;
using Aovias.Release.Contracts.RemoteDeployment;
using Aovias.Release.SDK;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aovias.DeploymentValidation.Tests;

/// <summary>
/// Sprint M2.7 regression suite: Fleet Registry (registration, search/filter, labels, concurrent
/// update), Fleet Heartbeat Cache, Fleet Event Store, offline detection, and
/// <c>FleetStatusService</c>'s cache-only aggregation (health/drift/reconciliation/metrics) plus
/// its three live-call actions (trigger reconciliation, maintenance enable/resume) against a fake
/// <see cref="IDeploymentClient"/> — never a real network call.
/// </summary>
public sealed class FleetManagementTests
{
    private static FleetRegistrationRequest BuildRegistrationRequest(string name = "Demo VPS", string baseUrl = "https://demo.example.test")
        => new()
        {
            Name = name,
            Description = "Test environment",
            BaseUrl = baseUrl,
            EnvironmentType = EnvironmentType.Demo,
            ApiKey = "test-api-key",
            Labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["tier"] = "web", ["region"] = "us" }
        };

    [Fact]
    public void Register_persists_environment_with_labels()
    {
        var registry = new FleetRegistry();
        var environment = registry.Register(BuildRegistrationRequest());

        Assert.NotNull(registry.Get(environment.Id));
        Assert.Equal("web", environment.Labels["tier"]);
        Assert.True(environment.IsRegistered);
    }

    [Fact]
    public void Search_filters_by_text_type_and_labels()
    {
        var registry = new FleetRegistry();
        var demo = registry.Register(BuildRegistrationRequest("Demo VPS", "https://demo.example.test"));
        var prod = registry.Register(new FleetRegistrationRequest
        {
            Name = "Production Primary",
            BaseUrl = "https://prod.example.test",
            EnvironmentType = EnvironmentType.Production,
            Labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["tier"] = "web", ["region"] = "eu" }
        });

        var byText = registry.Search(new FleetSearchQuery { Text = "demo" });
        Assert.Single(byText);
        Assert.Equal(demo.Id, byText[0].Id);

        var byType = registry.Search(new FleetSearchQuery { EnvironmentType = EnvironmentType.Production });
        Assert.Single(byType);
        Assert.Equal(prod.Id, byType[0].Id);

        var byLabel = registry.Search(new FleetSearchQuery
        {
            Labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["region"] = "eu" }
        });
        Assert.Single(byLabel);
        Assert.Equal(prod.Id, byLabel[0].Id);
    }

    [Fact]
    public void Update_applies_with_expression_and_returns_null_for_unknown_id()
    {
        var registry = new FleetRegistry();
        var environment = registry.Register(BuildRegistrationRequest());

        var updated = registry.Update(environment.Id, current => current with { MaintenanceMode = true, MaintenanceReason = "Patch window" });

        Assert.NotNull(updated);
        Assert.True(updated!.MaintenanceMode);
        Assert.Equal("Patch window", registry.Get(environment.Id)!.MaintenanceReason);

        Assert.Null(registry.Update("unknown-id", current => current));
    }

    [Fact]
    public void FleetOnlineStatus_reflects_last_heartbeat_against_threshold()
    {
        var registry = new FleetRegistry();
        var environment = registry.Register(BuildRegistrationRequest());
        var threshold = TimeSpan.FromSeconds(90);

        Assert.False(FleetOnlineStatus.IsOnline(environment, threshold));

        var withRecentHeartbeat = environment with { LastHeartbeatUtc = DateTimeOffset.UtcNow };
        Assert.True(FleetOnlineStatus.IsOnline(withRecentHeartbeat, threshold));

        var withStaleHeartbeat = environment with { LastHeartbeatUtc = DateTimeOffset.UtcNow.AddMinutes(-10) };
        Assert.False(FleetOnlineStatus.IsOnline(withStaleHeartbeat, threshold));
    }

    [Fact]
    public void FleetEventStore_returns_recent_events_filtered_by_environment()
    {
        var store = new InMemoryFleetEventStore();
        store.Append(new FleetEvent { Type = FleetEventType.AgentRegistered, EnvironmentId = "env-a", Message = "registered" });
        store.Append(new FleetEvent { Type = FleetEventType.AgentOnline, EnvironmentId = "env-b", Message = "online" });
        store.Append(new FleetEvent { Type = FleetEventType.DriftDetected, EnvironmentId = "env-a", Message = "drift" });

        var all = store.GetRecent(limit: 100);
        Assert.Equal(3, all.Count);

        var forEnvA = store.GetRecent("env-a", 100);
        Assert.Equal(2, forEnvA.Count);
        Assert.All(forEnvA, fleetEvent => Assert.Equal("env-a", fleetEvent.EnvironmentId));
    }

    [Fact]
    public void FleetHeartbeatCache_stores_and_removes_latest_heartbeat()
    {
        var cache = new InMemoryFleetHeartbeatCache();
        var heartbeat = BuildHeartbeat("env-a", driftCount: 2);

        cache.Set("env-a", heartbeat);
        Assert.Equal(2, cache.Get("env-a")!.Reconciliation!.DriftFindingCount);

        cache.Remove("env-a");
        Assert.Null(cache.Get("env-a"));
    }

    [Fact]
    public void GetFleetMetrics_computes_from_registry_and_cache_only()
    {
        var registry = new FleetRegistry();
        var cache = new InMemoryFleetHeartbeatCache();
        var online = registry.Register(BuildRegistrationRequest("Online Env", "https://online.example.test"));
        var offline = registry.Register(BuildRegistrationRequest("Offline Env", "https://offline.example.test"));

        registry.Update(online.Id, current => current with { LastHeartbeatUtc = DateTimeOffset.UtcNow, MaintenanceMode = true });
        cache.Set(online.Id, BuildHeartbeat(online.Id, driftCount: 3, approvalsPending: 1));

        var service = BuildService(registry, cache, new InMemoryFleetEventStore(), new ThrowingDeploymentClientFactory());
        var metrics = service.GetFleetMetrics();

        Assert.Equal(2, metrics.TotalEnvironments);
        Assert.Equal(1, metrics.OnlineEnvironments);
        Assert.Equal(1, metrics.OfflineEnvironments);
        Assert.Equal(1, metrics.EnvironmentsWithDrift);
        Assert.Equal(1, metrics.EnvironmentsInMaintenance);
        Assert.Equal(1, metrics.TotalOperatorApprovalsPending);
        Assert.False(FleetOnlineStatus.IsOnline(offline, TimeSpan.FromSeconds(90)));
    }

    [Fact]
    public void GetFleetDrift_synthesizes_findings_from_cached_reconciliation_summary_and_flags_unreachable()
    {
        var registry = new FleetRegistry();
        var cache = new InMemoryFleetHeartbeatCache();
        var withDrift = registry.Register(BuildRegistrationRequest("With Drift", "https://a.example.test"));
        var neverPolled = registry.Register(BuildRegistrationRequest("Never Polled", "https://b.example.test"));

        cache.Set(withDrift.Id, BuildHeartbeat(withDrift.Id, driftCount: 2));

        var service = BuildService(registry, cache, new InMemoryFleetEventStore(), new ThrowingDeploymentClientFactory());
        var snapshot = service.GetFleetDrift();

        Assert.Single(snapshot.Environments);
        Assert.Equal(withDrift.Id, snapshot.Environments[0].EnvironmentId);
        Assert.True(snapshot.Environments[0].RequiresReconciliation);
        Assert.NotEmpty(snapshot.Environments[0].Findings);
        Assert.Contains(neverPolled.Id, snapshot.UnreachableEnvironmentIds);
    }

    [Fact]
    public async Task TriggerReconciliationAsync_calls_target_agent_and_emits_event()
    {
        var registry = new FleetRegistry();
        var environment = registry.Register(BuildRegistrationRequest());
        var events = new InMemoryFleetEventStore();
        var fakeClient = new FakeDeploymentClient
        {
            ReconciliationReport = new PlatformReconciliationReport { GeneratedUtc = DateTimeOffset.UtcNow, Actions = [], RequiresOperatorAttention = false }
        };

        var service = BuildService(registry, new InMemoryFleetHeartbeatCache(), events, new SingleClientFactory(fakeClient));

        var report = await service.TriggerReconciliationAsync(environment.Id);

        Assert.False(report.RequiresOperatorAttention);
        Assert.Contains(events.GetRecent(environment.Id, 10), fleetEvent => fleetEvent.Type == FleetEventType.ReconciliationTriggered);
    }

    [Fact]
    public async Task SetMaintenanceModeAsync_updates_registry_cache_and_emits_event()
    {
        var registry = new FleetRegistry();
        var environment = registry.Register(BuildRegistrationRequest());
        var events = new InMemoryFleetEventStore();
        var fakeClient = new FakeDeploymentClient
        {
            MaintenanceStatus = new MaintenanceModeStatus { Active = true, Reason = "Patch window", ChangedUtc = DateTimeOffset.UtcNow, ChangedBy = "operator" }
        };

        var service = BuildService(registry, new InMemoryFleetHeartbeatCache(), events, new SingleClientFactory(fakeClient));

        var status = await service.SetMaintenanceModeAsync(environment.Id, new MaintenanceModeRequest { Reason = "Patch window", RequestedBy = "operator" });

        Assert.True(status.Active);
        Assert.True(registry.Get(environment.Id)!.MaintenanceMode);
        Assert.Contains(events.GetRecent(environment.Id, 10), fleetEvent => fleetEvent.Type == FleetEventType.MaintenanceEnabled);
    }

    [Fact]
    public async Task TriggerReconciliationAsync_throws_for_unknown_environment()
    {
        var service = BuildService(new FleetRegistry(), new InMemoryFleetHeartbeatCache(), new InMemoryFleetEventStore(), new ThrowingDeploymentClientFactory());

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.TriggerReconciliationAsync("unknown-id"));
    }

    private static RuntimeHeartbeatDto BuildHeartbeat(string environmentId, int driftCount, int approvalsPending = 0)
        => new()
        {
            EnvironmentId = environmentId,
            RuntimeVersion = "1.0.0",
            TimestampUtc = DateTimeOffset.UtcNow,
            HealthStatus = "Healthy",
            Reconciliation = new ReconciliationSummaryDto
            {
                LastRunUtc = DateTimeOffset.UtcNow,
                DriftFindingCount = driftCount,
                DriftBySeverity = driftCount > 0
                    ? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["Warning"] = driftCount }
                    : new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase),
                OperatorApprovalsPending = approvalsPending,
                LastCycleSucceeded = true,
                LastCycleDurationMs = 12.5
            }
        };

    private static FleetStatusService BuildService(
        IFleetRegistry registry,
        IFleetHeartbeatCache cache,
        IFleetEventStore events,
        IFleetDeploymentClientFactory clientFactory)
        => new(registry, cache, clientFactory, events, new StaticOptionsMonitor<Aovias.Configuration.Settings.CommandCenterSettings>(new Aovias.Configuration.Settings.CommandCenterSettings()));

    /// <summary>Minimal <see cref="IOptionsMonitor{TOptions}"/> test double — no DI container required.</summary>
    private sealed class StaticOptionsMonitor<T> : IOptionsMonitor<T>
    {
        public StaticOptionsMonitor(T value) => CurrentValue = value;

        public T CurrentValue { get; }

        public T Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }

    private sealed class ThrowingDeploymentClientFactory : IFleetDeploymentClientFactory
    {
        public IDeploymentClient CreateClient(RemoteEnvironment environment) => throw new InvalidOperationException("This test never expects a live call.");
    }

    private sealed class SingleClientFactory : IFleetDeploymentClientFactory
    {
        private readonly IDeploymentClient client;

        public SingleClientFactory(IDeploymentClient client) => this.client = client;

        public IDeploymentClient CreateClient(RemoteEnvironment environment) => client;
    }

    /// <summary>A fake <see cref="IDeploymentClient"/> — only the members Sprint M2.7's tests exercise return configured values; everything else throws, so an accidental unconfigured call fails loudly rather than silently returning a default.</summary>
    private sealed class FakeDeploymentClient : IDeploymentClient
    {
        public PlatformReconciliationReport? ReconciliationReport { get; set; }

        public MaintenanceModeStatus? MaintenanceStatus { get; set; }

        public IReadOnlyList<PlatformDeploymentHistoryEntry> History { get; set; } = [];

        public Task<PlatformReconciliationReport> ReconcileAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(ReconciliationReport ?? throw new InvalidOperationException("ReconciliationReport not configured."));

        public Task<MaintenanceModeStatus> GetMaintenanceModeAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(MaintenanceStatus ?? throw new InvalidOperationException("MaintenanceStatus not configured."));

        public Task<MaintenanceModeStatus> SetMaintenanceModeAsync(MaintenanceModeRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(MaintenanceStatus ?? throw new InvalidOperationException("MaintenanceStatus not configured."));

        public Task<MaintenanceModeStatus> ResumeFromMaintenanceAsync(string resumedBy, CancellationToken cancellationToken = default)
            => Task.FromResult(MaintenanceStatus ?? throw new InvalidOperationException("MaintenanceStatus not configured."));

        public Task<IReadOnlyList<PlatformDeploymentHistoryEntry>> GetHistoryAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(History);

        public Task<RuntimeHeartbeatDto> GetHeartbeatAsync(CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<PlatformDeploymentResponse> DeployPlatformAsync(PlatformDeploymentRequest request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<PlatformDeploymentResponse> UploadAndDeployPackageAsync(string packagePath, string runtime, string environment, string uploadedBy, bool operatorConfirmed = true, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<DeploymentStatusResponse?> GetStatusAsync(string deploymentId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<IReadOnlyList<string>> GetLogsAsync(string deploymentId, string? serviceId = null, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<PlatformDeploymentReport?> GetReportAsync(string deploymentId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<bool> CancelAsync(CancelDeploymentRequest request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<PlatformDeploymentResponse> RollbackAsync(PlatformRollbackRequest request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<AgentRegistrationResponse> RegisterEnvironmentAsync(AgentRegistrationRequest request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<EnvironmentHealthSnapshot?> GetEnvironmentHealthAsync(string environmentId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<EnvironmentDriftReport> DetectDriftAsync(string environmentId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<DeploymentReliabilityMetrics> GetReliabilityMetricsAsync(CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<ChunkedUploadSession> InitiateChunkedUploadAsync(InitiateChunkedUploadRequest request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<ChunkedUploadSession> UploadChunkAsync(string uploadId, int chunkIndex, UploadChunkMetadata metadata, Stream chunkStream, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<ChunkedUploadSession> GetUploadSessionAsync(string uploadId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<UploadPackageResult> CompleteChunkedUploadAsync(string uploadId, string runtime, string environment, string uploadedBy, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }
}
