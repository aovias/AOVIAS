using Aovias.CommandCenter.Infrastructure;
using Aovias.Configuration.Deployment;
using Aovias.Configuration.Features;
using Aovias.Configuration.Settings;
using Aovias.Release.Contracts.Releases;
using Aovias.Release.Contracts.RemoteDeployment;
using Aovias.Runtime.Agent.Infrastructure.Deployment;
using Aovias.Runtime.Agent.Infrastructure.Registration;
using Aovias.Runtime.Agent.Infrastructure.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aovias.DeploymentValidation.Tests;

/// <summary>
/// Sprint M4.3 regression suite: automatic environment registration, capability declaration, and
/// idempotency. Split across the three layers the feature touches â€” <see cref="FleetRegistry"/>'s
/// new BaseUrl-based upsert (server-side duplicate prevention), <see cref="RuntimeAgentPollingService"/>'s
/// extended heartbeat sync (capability/platform-version propagation), and
/// <see cref="EnvironmentRegistrationHostedService"/>'s registration decision tree (client-side
/// idempotency/restart-survival/retry) â€” each exercised against the real class, never a re-
/// implementation of its logic.
/// </summary>
public sealed class EnvironmentRegistrationTests
{
    private static FleetRegistrationRequest BuildRequest(string baseUrl, IReadOnlyList<string>? capabilities = null)
        => new()
        {
            Name = "Auto Env",
            BaseUrl = baseUrl,
            EnvironmentType = EnvironmentType.Demo,
            ApiKey = "key-1",
            Capabilities = capabilities ?? ["runtime", "release-management"]
        };

    [Fact]
    public void Register_persists_capabilities_from_request()
    {
        var registry = new FleetRegistry();

        var environment = registry.Register(BuildRequest("https://a.example.test", ["runtime", "docker-provider"]));

        Assert.Equal(["runtime", "docker-provider"], environment.Capabilities);
    }

    [Fact]
    public void Register_with_same_base_url_updates_in_place_instead_of_duplicating()
    {
        var registry = new FleetRegistry();

        var first = registry.Register(BuildRequest("https://demo.example.test", ["runtime"]));
        var second = registry.Register(BuildRequest("https://demo.example.test", ["runtime", "rollback"]));

        Assert.Equal(first.Id, second.Id);
        Assert.Single(registry.List());
        Assert.Equal(["runtime", "rollback"], registry.Get(first.Id)!.Capabilities);
    }

    [Fact]
    public void Register_with_same_base_url_does_not_erase_heartbeat_only_state()
    {
        var registry = new FleetRegistry();
        var first = registry.Register(BuildRequest("https://demo.example.test"));

        // Simulate a heartbeat having already run before the agent restarts and re-registers.
        registry.Update(first.Id, current => current with
        {
            RuntimeVersion = "2.3.0",
            PlatformVersion = "1.4.0",
            HealthStatus = "Healthy",
            LastHeartbeatUtc = DateTimeOffset.UtcNow
        });

        var reRegistered = registry.Register(BuildRequest("https://demo.example.test"));

        Assert.Equal(first.Id, reRegistered.Id);
        Assert.Equal("2.3.0", reRegistered.RuntimeVersion);
        Assert.Equal("1.4.0", reRegistered.PlatformVersion);
        Assert.Equal("Healthy", reRegistered.HealthStatus);
        Assert.NotNull(reRegistered.LastHeartbeatUtc);
    }

    [Fact]
    public void Register_with_different_base_urls_creates_distinct_environments()
    {
        var registry = new FleetRegistry();

        var a = registry.Register(BuildRequest("https://a.example.test"));
        var b = registry.Register(BuildRequest("https://b.example.test"));

        Assert.NotEqual(a.Id, b.Id);
        Assert.Equal(2, registry.List().Count);
    }

    [Fact]
    public async Task PollOnceAsync_synchronizes_capabilities_and_platform_version_from_heartbeat()
    {
        var registry = new FleetRegistry();
        var environment = registry.Register(BuildRequest("https://demo.example.test", []));
        var cache = new InMemoryFleetHeartbeatCache();
        var events = new InMemoryFleetEventStore();
        var heartbeat = new RuntimeHeartbeatDto
        {
            EnvironmentId = environment.Id,
            RuntimeVersion = "2.0.0",
            PlatformVersion = "3.1.0",
            HealthStatus = "Healthy",
            Capabilities = ["runtime", "release-management", "rollback"]
        };
        var service = new RuntimeAgentPollingService(
            registry,
            new SingleHeartbeatClientFactory(new FakeHeartbeatOnlyDeploymentClient(heartbeat)),
            cache,
            events,
            new StaticOptionsMonitor<CommandCenterSettings>(new CommandCenterSettings()),
            NullLogger<RuntimeAgentPollingService>.Instance);

        await service.PollOnceAsync(CancellationToken.None);

        var updated = registry.Get(environment.Id)!;
        Assert.Equal(["runtime", "release-management", "rollback"], updated.Capabilities);
        Assert.Equal("3.1.0", updated.PlatformVersion);
        Assert.Equal("2.0.0", updated.RuntimeVersion);
    }

    [Fact]
    public async Task EnsureRegisteredAsync_first_run_registers_with_command_center_and_persists_record()
    {
        var harness = new RegistrationHarness();

        await harness.HostedService.EnsureRegisteredAsync(harness.Settings, CancellationToken.None);

        Assert.Equal(1, harness.CommandCenterClient.RegisterCallCount);
        Assert.NotNull(harness.CommandCenterClient.LastRegisterRequest);
        // Sprint G1: first real execution of this test. RuntimeCapability.* (Orchestration.Contracts)
        // is the actual, PascalCase capability vocabulary RuntimeCapabilitiesProvider emits — a
        // deliberately disjoint vocabulary from the lowercase-hyphenated Capability/CapabilityRegistry
        // ids used elsewhere (see ROADMAP.md M4.4 note). The harness enables Runtime+ReleaseManagement,
        // which together produce these 7 capabilities; the stale lowercase 2-item list was never
        // checked against the real provider before this sprint.
        Assert.Equal(
            ["Runtime", "HealthMonitoring", "ReleaseManagement", "WorkflowEngine", "Rollback", "RemoteDeploymentOrchestration", "PlatformCertification"],
            harness.CommandCenterClient.LastRegisterRequest!.Capabilities);
        Assert.False(string.IsNullOrWhiteSpace(harness.CommandCenterClient.LastRegisterRequest!.ApiKey));

        var record = await harness.RecordStore.TryLoadAsync();
        Assert.NotNull(record);
        Assert.Equal(harness.CommandCenterClient.LastRegisteredEnvironmentId, record!.EnvironmentId);
    }

    [Fact]
    public async Task EnsureRegisteredAsync_does_not_reregister_when_command_center_still_knows_the_environment()
    {
        var harness = new RegistrationHarness();
        await harness.HostedService.EnsureRegisteredAsync(harness.Settings, CancellationToken.None);
        Assert.Equal(1, harness.CommandCenterClient.RegisterCallCount);

        harness.CommandCenterClient.KnownEnvironmentId = harness.CommandCenterClient.LastRegisteredEnvironmentId;
        await harness.HostedService.EnsureRegisteredAsync(harness.Settings, CancellationToken.None);

        // RegisterAsync (Fleet Registry) is only ever called from within the same code path that
        // calls self-registration first â€” an unchanged RegisterCallCount is proof neither step ran.
        Assert.Equal(1, harness.CommandCenterClient.RegisterCallCount);
    }

    [Fact]
    public async Task EnsureRegisteredAsync_reregisters_when_command_center_no_longer_knows_the_environment()
    {
        var harness = new RegistrationHarness();
        await harness.HostedService.EnsureRegisteredAsync(harness.Settings, CancellationToken.None);
        Assert.Equal(1, harness.CommandCenterClient.RegisterCallCount);

        // Command Center restarted and lost its in-memory Fleet Registry (Sprint M2.7 accepted gap):
        // TryGetAsync now returns null for the previously-issued id.
        harness.CommandCenterClient.KnownEnvironmentId = null;
        await harness.HostedService.EnsureRegisteredAsync(harness.Settings, CancellationToken.None);

        Assert.Equal(2, harness.CommandCenterClient.RegisterCallCount);
    }

    /// <summary>Wires a real <see cref="EnvironmentRegistrationHostedService"/> against real <see cref="EnvironmentRegistrationStore"/>/<see cref="EnvironmentRegistrationRecordStore"/>/<see cref="RuntimeCapabilitiesProvider"/> and only fakes the one genuine external touchpoint, <see cref="ICommandCenterRegistrationClient"/>.</summary>
    private sealed class RegistrationHarness : IDisposable
    {
        private readonly string tempDirectory = Path.Combine(Path.GetTempPath(), "m4.3-registration-tests", Guid.NewGuid().ToString("N"));

        public RegistrationHarness()
        {
            Directory.CreateDirectory(tempDirectory);
            var pathResolver = new FakeDeploymentPathResolver(tempDirectory);
            RecordStore = new EnvironmentRegistrationRecordStore(pathResolver, NullLogger<EnvironmentRegistrationRecordStore>.Instance);
            CommandCenterClient = new FakeCommandCenterRegistrationClient();

            Settings = new EnvironmentRegistrationSettings
            {
                Enabled = true,
                CommandCenterBaseUrl = "https://command-center.example.test",
                RegistrationToken = "demo-vps-registration-token",
                EnvironmentType = "Demo",
                RetryIntervalSeconds = 5
            };

            var capabilitiesProvider = new RuntimeCapabilitiesProvider(
                new PlatformFeatureActivation { Runtime = true, ReleaseManagement = true },
                new StaticOptionsMonitor<SignalRSettings>(new SignalRSettings()),
                new StaticOptionsMonitor<ProviderSettings>(new ProviderSettings()));

            HostedService = new EnvironmentRegistrationHostedService(
                new StaticOptionsMonitor<EnvironmentRegistrationSettings>(Settings),
                new StaticOptionsMonitor<RuntimeSettings>(new RuntimeSettings
                {
                    ServerName = "Demo VPS",
                    Hostname = "demo-vps.example.test",
                    RuntimeVersion = "1.0.0",
                    PlatformVersion = "1.0.0",
                    AgentDisplayName = "Demo VPS"
                }),
                new StaticOptionsMonitor<ApiSettings>(new ApiSettings { PublicUrl = "https://demo-vps.example.test" }),
                new EnvironmentRegistrationStore(),
                RecordStore,
                CommandCenterClient,
                capabilitiesProvider,
                NullLogger<EnvironmentRegistrationHostedService>.Instance);
        }

        public EnvironmentRegistrationSettings Settings { get; }

        public EnvironmentRegistrationRecordStore RecordStore { get; }

        public FakeCommandCenterRegistrationClient CommandCenterClient { get; }

        public EnvironmentRegistrationHostedService HostedService { get; }

        public void Dispose()
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }

    private sealed class FakeDeploymentPathResolver(string configRoot) : IDeploymentPathResolver
    {
        public string DeploymentRoot => throw new NotSupportedException();
        public string IncomingRoot => throw new NotSupportedException();
        public string ReleaseRoot => throw new NotSupportedException();
        public string TempRoot => throw new NotSupportedException();
        public string ArchiveRoot => throw new NotSupportedException();
        public string StagingRoot => throw new NotSupportedException();
        public string ExtractionRoot => throw new NotSupportedException();
        public string ActiveRoot => throw new NotSupportedException();
        public string RollbackRoot => throw new NotSupportedException();
        public string AuditRoot => throw new NotSupportedException();
        public string RuntimeRoot => throw new NotSupportedException();
        public string ConfigRoot { get; } = configRoot;
        public string LogsRoot => throw new NotSupportedException();
        public string PackagesRoot => throw new NotSupportedException();
        public string CertificatesRoot => throw new NotSupportedException();
        public string ProfilesRoot => throw new NotSupportedException();
        public string SecretsRoot => throw new NotSupportedException();
        public string ResolveUnderDeploymentRoot(string relativeSegment) => throw new NotSupportedException();
        public void EnsureDirectory(string path) => Directory.CreateDirectory(path);
        public DeploymentStorageValidationResult ValidateStorageLayout(bool createMissing = false) => throw new NotSupportedException();
        public DeploymentStorageValidationResult ValidateWritable(string path, string label, string phase, string operation) => throw new NotSupportedException();
    }

    private sealed class FakeCommandCenterRegistrationClient : ICommandCenterRegistrationClient
    {
        public int RegisterCallCount { get; private set; }

        public FleetRegistrationRequest? LastRegisterRequest { get; private set; }

        public string? LastRegisteredEnvironmentId { get; private set; }

        /// <summary>When set, <see cref="TryGetAsync"/> returns a non-null environment for this id â€” simulating "Command Center still remembers this registration."</summary>
        public string? KnownEnvironmentId { get; set; }

        public Task<RemoteEnvironment> RegisterAsync(string commandCenterBaseUrl, FleetRegistrationRequest request, CancellationToken cancellationToken)
        {
            RegisterCallCount++;
            LastRegisterRequest = request;
            LastRegisteredEnvironmentId = $"env-{RegisterCallCount}";
            KnownEnvironmentId = LastRegisteredEnvironmentId;

            return Task.FromResult(new RemoteEnvironment
            {
                Id = LastRegisteredEnvironmentId,
                Name = request.Name,
                BaseUrl = request.BaseUrl,
                RuntimeAgentEndpoint = request.BaseUrl,
                EnvironmentType = request.EnvironmentType,
                ApiKey = request.ApiKey,
                Capabilities = request.Capabilities,
                IsRegistered = true
            });
        }

        public Task<RemoteEnvironment?> TryGetAsync(string commandCenterBaseUrl, string environmentId, CancellationToken cancellationToken)
            => Task.FromResult(string.Equals(environmentId, KnownEnvironmentId, StringComparison.Ordinal)
                ? new RemoteEnvironment { Id = environmentId, Name = "known", BaseUrl = "https://known.example.test", RuntimeAgentEndpoint = "https://known.example.test" }
                : null);
    }

    /// <summary>Mirrors <c>FleetManagementTests.SingleClientFactory</c>'s pattern (a private, per-file copy â€” the established house convention for test doubles) so <see cref="RuntimeAgentPollingService"/> always gets the one configured fake client regardless of which environment it is polling.</summary>
    private sealed class SingleHeartbeatClientFactory(Aovias.Release.SDK.IDeploymentClient client) : IFleetDeploymentClientFactory
    {
        public Aovias.Release.SDK.IDeploymentClient CreateClient(RemoteEnvironment environment) => client;
    }

    private sealed class FakeHeartbeatOnlyDeploymentClient(RuntimeHeartbeatDto heartbeat) : Aovias.Release.SDK.IDeploymentClient
    {
        // Only GetHeartbeatAsync is exercised by RuntimeAgentPollingService; everything else throws
        // so an accidental unconfigured call fails loudly (same discipline as FleetManagementTests'
        // own FakeDeploymentClient).
        public Task<RuntimeHeartbeatDto> GetHeartbeatAsync(CancellationToken cancellationToken = default) => Task.FromResult(heartbeat);
        public Task<PlatformReconciliationReport> ReconcileAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<MaintenanceModeStatus> GetMaintenanceModeAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<MaintenanceModeStatus> SetMaintenanceModeAsync(MaintenanceModeRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<MaintenanceModeStatus> ResumeFromMaintenanceAsync(string resumedBy, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<PlatformDeploymentHistoryEntry>> GetHistoryAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PlatformDeploymentResponse> DeployPlatformAsync(PlatformDeploymentRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PlatformDeploymentResponse> UploadAndDeployPackageAsync(string packagePath, string runtime, string environment, string uploadedBy, bool operatorConfirmed = true, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<DeploymentStatusResponse?> GetStatusAsync(string deploymentId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<string>> GetLogsAsync(string deploymentId, string? serviceId = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PlatformDeploymentReport?> GetReportAsync(string deploymentId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> CancelAsync(CancelDeploymentRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PlatformDeploymentResponse> RollbackAsync(PlatformRollbackRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<AgentRegistrationResponse> RegisterEnvironmentAsync(AgentRegistrationRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<EnvironmentHealthSnapshot?> GetEnvironmentHealthAsync(string environmentId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<EnvironmentDriftReport> DetectDriftAsync(string environmentId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<DeploymentReliabilityMetrics> GetReliabilityMetricsAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ChunkedUploadSession> InitiateChunkedUploadAsync(InitiateChunkedUploadRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ChunkedUploadSession> UploadChunkAsync(string uploadId, int chunkIndex, UploadChunkMetadata metadata, Stream chunkStream, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ChunkedUploadSession> GetUploadSessionAsync(string uploadId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<UploadPackageResult> CompleteChunkedUploadAsync(string uploadId, string runtime, string environment, string uploadedBy, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class StaticOptionsMonitor<T>(T value) : IOptionsMonitor<T>
    {
        public T CurrentValue { get; } = value;
        public T Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
