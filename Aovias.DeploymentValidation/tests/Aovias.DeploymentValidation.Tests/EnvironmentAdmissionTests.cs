using Aovias.Configuration.Deployment;
using Aovias.Configuration.Features;
using Aovias.Configuration.Settings;
using Aovias.Deployment.Contracts;
using Aovias.Deployment.Contracts.Admission;
using Aovias.Deployment.Contracts.Capabilities;
using Aovias.Deployment.Contracts.Infrastructure;
using Aovias.Deployment.Contracts.Migration;
using Aovias.Deployment.Contracts.Planning;
using Aovias.Deployment.Contracts.Reconciliation;
using Aovias.Runtime.Agent.Application.Interfaces;
using Aovias.Runtime.Agent.Infrastructure.Admission;
using Aovias.Runtime.Agent.Infrastructure.Reconciliation;
using Aovias.Runtime.Agent.Infrastructure.Reliability;
using Aovias.Runtime.Agent.Infrastructure.SignalR;
using Aovias.Runtime.Contracts.Health;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aovias.DeploymentValidation.Tests;

/// <summary>
/// Sprint M4.4 — Environment Admission &amp; Deployment Readiness regression tests. Covers three
/// layers, matching the M4.4 brief's verification list:
/// <list type="bullet">
/// <item><see cref="EnvironmentAdmissionEvaluator"/> — the pure four-state classification (fresh
/// environment, missing capability rejection, capability version mismatch, successful admission,
/// external, deferrable).</item>
/// <item><see cref="EnvironmentAdmissionStore"/> — persistence/restart.</item>
/// <item><see cref="EnvironmentAdmissionHostedService"/> — readiness recalculation after a
/// deployment and after a capability profile change, and <see cref="RuntimeHeartbeatService"/>'s
/// consumption of the persisted report.</item>
/// </list>
/// </summary>
public sealed class EnvironmentAdmissionTests
{
    // ---- EnvironmentAdmissionEvaluator: pure classification ----

    [Fact]
    public void Evaluate_FreshEnvironment_IsReadyWithNoDiagnostics()
    {
        var report = EnvironmentAdmissionEvaluator.Evaluate("docker-host", new Dictionary<string, IReadOnlyList<Requirement>>());

        Assert.True(report.IsReady);
        Assert.Empty(report.Diagnostics);
        Assert.Empty(report.ServiceIdsEvaluated);
        Assert.Equal("docker-host", report.CapabilityProfileId);
    }

    [Fact]
    public void Evaluate_SatisfiedRequirement_ClassifiesAsSatisfied_AndIsReady()
    {
        var requirements = new Dictionary<string, IReadOnlyList<Requirement>>
        {
            ["gateway"] = [new Requirement { Id = "primary-database", Category = RequirementCategories.SqlDatabase, Mandatory = true }]
        };

        var report = EnvironmentAdmissionEvaluator.Evaluate("docker-host", requirements);

        Assert.True(report.IsReady);
        var diagnostic = Assert.Single(report.Diagnostics);
        Assert.Equal(EnvironmentRequirementStatus.Satisfied, diagnostic.Status);
        Assert.Equal(1, report.SatisfiedCount);
        Assert.Equal(0, report.MissingCount);
    }

    [Fact]
    public void Evaluate_MissingMandatoryRequirement_ClassifiesAsMissing_AndNotReady()
    {
        // object-storage is not declared by any embedded EnvironmentCapabilityProfile (Sprint M2.3 D6 catalog).
        var requirements = new Dictionary<string, IReadOnlyList<Requirement>>
        {
            ["identity"] = [new Requirement { Id = "attachments", Category = RequirementCategories.ObjectStorage, Mandatory = true }]
        };

        var report = EnvironmentAdmissionEvaluator.Evaluate("docker-host", requirements);

        Assert.False(report.IsReady);
        var diagnostic = Assert.Single(report.Diagnostics);
        Assert.Equal(EnvironmentRequirementStatus.Missing, diagnostic.Status);
        Assert.Equal("identity", diagnostic.ServiceId);
        Assert.Single(report.Blockers);
        Assert.Contains("attachments", report.Blockers[0]);
    }

    [Fact]
    public void Evaluate_IncompatibleCapabilityVersion_ClassifiesAsMissing_AndNotReady()
    {
        // docker-host's sqlserver capability is version 2022.0.0 (capability-registry.json) — too old for this constraint.
        var requirements = new Dictionary<string, IReadOnlyList<Requirement>>
        {
            ["gateway"] = [new Requirement { Id = "primary-database", Category = RequirementCategories.SqlDatabase, Mandatory = true, VersionConstraint = ">=2025.0" }]
        };

        var report = EnvironmentAdmissionEvaluator.Evaluate("docker-host", requirements);

        Assert.False(report.IsReady);
        Assert.Equal(EnvironmentRequirementStatus.Missing, report.Diagnostics.Single().Status);
    }

    [Fact]
    public void Evaluate_UnmetNonMandatoryRequirement_ClassifiesAsDeferrable_AndIsStillReady()
    {
        var requirements = new Dictionary<string, IReadOnlyList<Requirement>>
        {
            ["reporting"] = [new Requirement { Id = "cold-storage", Category = RequirementCategories.ObjectStorage, Mandatory = false }]
        };

        var report = EnvironmentAdmissionEvaluator.Evaluate("docker-host", requirements);

        Assert.True(report.IsReady);
        Assert.Equal(EnvironmentRequirementStatus.Deferrable, report.Diagnostics.Single().Status);
        Assert.Equal(1, report.DeferrableCount);
    }

    [Fact]
    public void Evaluate_UnmetExternalAiProviderRequirement_ClassifiesAsExternal_EvenWhenMandatory()
    {
        // docker-host declares no external-ai-provider capability, but this category is never a blocker.
        var requirements = new Dictionary<string, IReadOnlyList<Requirement>>
        {
            ["assistant"] = [new Requirement { Id = "llm", Category = RequirementCategories.ExternalAiProvider, Mandatory = true }]
        };

        var report = EnvironmentAdmissionEvaluator.Evaluate("docker-host", requirements);

        Assert.True(report.IsReady);
        Assert.Equal(EnvironmentRequirementStatus.External, report.Diagnostics.Single().Status);
        Assert.Equal(1, report.ExternalCount);
    }

    [Fact]
    public void Evaluate_UnderRicherProfile_SameRequirementBecomesSatisfied()
    {
        // metrics (prometheus) is absent from docker-host but present in production-vps.
        var requirements = new Dictionary<string, IReadOnlyList<Requirement>>
        {
            ["gateway"] = [new Requirement { Id = "metrics-scrape", Category = RequirementCategories.Metrics, Mandatory = true }]
        };

        var underDockerHost = EnvironmentAdmissionEvaluator.Evaluate("docker-host", requirements);
        var underProductionVps = EnvironmentAdmissionEvaluator.Evaluate("production-vps", requirements);

        Assert.False(underDockerHost.IsReady);
        Assert.True(underProductionVps.IsReady);
    }

    [Fact]
    public void Evaluate_MultipleServices_AggregatesDiagnosticsAcrossAll()
    {
        var requirements = new Dictionary<string, IReadOnlyList<Requirement>>
        {
            ["gateway"] = [new Requirement { Id = "primary-database", Category = RequirementCategories.SqlDatabase, Mandatory = true }],
            ["identity"] = [new Requirement { Id = "attachments", Category = RequirementCategories.ObjectStorage, Mandatory = true }]
        };

        var report = EnvironmentAdmissionEvaluator.Evaluate("docker-host", requirements);

        Assert.False(report.IsReady);
        Assert.Equal(2, report.Diagnostics.Count);
        Assert.Equal(["gateway", "identity"], report.ServiceIdsEvaluated);
    }

    [Fact]
    public void Evaluate_UnknownCapabilityProfileId_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            EnvironmentAdmissionEvaluator.Evaluate("no-such-profile", new Dictionary<string, IReadOnlyList<Requirement>>()));
    }

    // ---- EnvironmentAdmissionStore: persistence / restart ----

    [Fact]
    public async Task Store_TryLoad_ReturnsNullWhenNoFileExists()
    {
        using var harness = new AdmissionHarness();
        Assert.Null(await harness.AdmissionStore.TryLoadAsync());
    }

    [Fact]
    public async Task Store_SaveThenLoad_RoundTrips()
    {
        using var harness = new AdmissionHarness();
        var report = EnvironmentAdmissionEvaluator.Evaluate("docker-host", new Dictionary<string, IReadOnlyList<Requirement>>
        {
            ["gateway"] = [new Requirement { Id = "primary-database", Category = RequirementCategories.SqlDatabase, Mandatory = true }]
        });

        await harness.AdmissionStore.SaveAsync(report);
        var loaded = await harness.AdmissionStore.TryLoadAsync();

        Assert.NotNull(loaded);
        Assert.Equal(report.IsReady, loaded!.IsReady);
        Assert.Equal(report.CapabilityProfileId, loaded.CapabilityProfileId);
        Assert.Single(loaded.Diagnostics);
    }

    [Fact]
    public async Task Store_SimulatedRestart_NewStoreInstanceReadsPersistedReport()
    {
        using var harness = new AdmissionHarness();
        var report = EnvironmentAdmissionEvaluator.Evaluate("docker-host", new Dictionary<string, IReadOnlyList<Requirement>>());
        await harness.AdmissionStore.SaveAsync(report);

        // A fresh EnvironmentAdmissionStore instance, pointed at the same ConfigRoot, is exactly
        // what happens on a real Runtime Agent process restart (Sprint M4.4: "admission refresh
        // after Runtime Agent restart") — the report is not recomputed from scratch, it survives.
        var restarted = new EnvironmentAdmissionStore(harness.PathResolver, NullLogger<EnvironmentAdmissionStore>.Instance);
        var loaded = await restarted.TryLoadAsync();

        Assert.NotNull(loaded);
        Assert.True(loaded!.IsReady);
    }

    // ---- EnvironmentAdmissionHostedService: recalculation triggers ----

    [Fact]
    public async Task EvaluateOnceAsync_FreshEnvironment_PersistsReadyReport()
    {
        using var harness = new AdmissionHarness();

        await harness.HostedService.EvaluateOnceAsync(CancellationToken.None);

        var report = await harness.AdmissionStore.TryLoadAsync();
        Assert.NotNull(report);
        Assert.True(report!.IsReady);
        Assert.Empty(report.Diagnostics);
    }

    [Fact]
    public async Task EvaluateOnceAsync_AfterDeploymentRecordsUnmetRequirement_RecalculatesToNotReady()
    {
        using var harness = new AdmissionHarness();
        await harness.HostedService.EvaluateOnceAsync(CancellationToken.None);
        Assert.True((await harness.AdmissionStore.TryLoadAsync())!.IsReady);

        // Simulate a completed deployment (Sprint M2.6 D1's own write path) declaring a requirement
        // docker-host cannot satisfy.
        harness.PlanStore.Record("identity", BuildRecord("identity", [
            new Requirement { Id = "attachments", Category = RequirementCategories.ObjectStorage, Mandatory = true }
        ]));

        await harness.HostedService.EvaluateOnceAsync(CancellationToken.None);

        var report = await harness.AdmissionStore.TryLoadAsync();
        Assert.False(report!.IsReady);
        Assert.Equal(["identity"], report.ServiceIdsEvaluated);
    }

    [Fact]
    public async Task EvaluateOnceAsync_AfterCapabilityProfileChange_RecalculatesToReady()
    {
        using var harness = new AdmissionHarness();
        harness.PlanStore.Record("gateway", BuildRecord("gateway", [
            new Requirement { Id = "metrics-scrape", Category = RequirementCategories.Metrics, Mandatory = true }
        ]));

        await harness.HostedService.EvaluateOnceAsync(CancellationToken.None);
        Assert.False((await harness.AdmissionStore.TryLoadAsync())!.IsReady);

        // "Admission refresh on capability changes" (M4.4 scope) — IOptionsMonitor.CurrentValue is
        // re-read on every cycle, so a changed CapabilityProfileId is picked up without restarting.
        harness.RegistrationSettings.Current = new EnvironmentRegistrationSettings { CapabilityProfileId = "production-vps" };
        await harness.HostedService.EvaluateOnceAsync(CancellationToken.None);

        Assert.True((await harness.AdmissionStore.TryLoadAsync())!.IsReady);
    }

    [Fact]
    public async Task EvaluateOnceAsync_UnknownCapabilityProfileId_DoesNotThrow_AndDoesNotOverwritePreviousReport()
    {
        using var harness = new AdmissionHarness();
        await harness.HostedService.EvaluateOnceAsync(CancellationToken.None);
        var before = await harness.AdmissionStore.TryLoadAsync();

        harness.RegistrationSettings.Current = new EnvironmentRegistrationSettings { CapabilityProfileId = "no-such-profile" };
        await harness.HostedService.EvaluateOnceAsync(CancellationToken.None);

        var after = await harness.AdmissionStore.TryLoadAsync();
        Assert.Equal(before!.CapabilityProfileId, after!.CapabilityProfileId);
    }

    // ---- RuntimeHeartbeatService: consumes the persisted report ----

    [Fact]
    public async Task GetHeartbeatAsync_WithPersistedNotReadyReport_ReportsDeploymentReadyFalseAndBlockerCount()
    {
        using var harness = new AdmissionHarness();
        var notReady = EnvironmentAdmissionEvaluator.Evaluate("docker-host", new Dictionary<string, IReadOnlyList<Requirement>>
        {
            ["identity"] = [
                new Requirement { Id = "attachments", Category = RequirementCategories.ObjectStorage, Mandatory = true },
                new Requirement { Id = "search-index", Category = "search-engine", Mandatory = true }
            ]
        });
        await harness.AdmissionStore.SaveAsync(notReady);

        var heartbeat = await harness.HeartbeatService.GetHeartbeatAsync();

        Assert.False(heartbeat.DeploymentReady);
        Assert.Equal(2, heartbeat.AdmissionBlockerCount);
    }

    [Fact]
    public async Task GetHeartbeatAsync_WithNoPersistedReportYet_DefaultsToReady()
    {
        using var harness = new AdmissionHarness();

        var heartbeat = await harness.HeartbeatService.GetHeartbeatAsync();

        Assert.True(heartbeat.DeploymentReady);
        Assert.Equal(0, heartbeat.AdmissionBlockerCount);
    }

    private static LastDeploymentRecord BuildRecord(string serviceId, IReadOnlyList<Requirement> requirements)
    {
        var contract = new InfrastructureContract
        {
            ServiceId = serviceId,
            SchemaVersion = ContractSchemaVersions.Current,
            ContractVersion = ContractSchemaVersions.ContractVersionV2,
            Compatibility = new ContractCompatibilityMetadata
            {
                MinimumSupportedSchemaVersion = ContractSchemaVersions.V1,
                MaximumSupportedSchemaVersion = ContractSchemaVersions.Current
            },
            Integrity = new ContractIntegrityMetadata { ContentHash = "test-content-hash" },
            Requirements = requirements
        };

        var plan = new DeploymentPlan
        {
            Metadata = new DeploymentPlanMetadata
            {
                PlanId = $"{serviceId}:1.0.0:test-content-hash",
                ServiceId = serviceId,
                ServiceVersion = "1.0.0",
                SchemaVersion = ContractSchemaVersions.Current,
                CorrelationId = "corr-1",
                GeneratedUtc = DateTimeOffset.UtcNow
            },
            Environment = new DeploymentPlanEnvironment { EnvironmentId = "local" },
            Services = [new DeploymentPlanServiceEntry { ServiceId = serviceId, Required = true, Type = "primary" }],
            Infrastructure = new DeploymentPlanInfrastructure { Requirements = requirements },
            Dependencies = [serviceId],
            ExecutionPhases = [],
            HealthGates = [],
            RollbackPoints = [],
            CertificationGates = [],
            Recovery = new RecoveryStrategy { Type = RecoveryStrategyType.None, Description = "No recovery configured for this test plan." },
            Diagnostics = new PlanDiagnostics { AdmissionCorrelationId = "corr-1", ContentHash = "test-content-hash" }
        };

        var manifest = new ServiceManifest
        {
            ServiceId = serviceId,
            DisplayName = serviceId,
            Version = "1.0.0",
            Docker = new ServiceDockerSpecification { Image = $"aovias/{serviceId}:1.0.0", HealthEndpoint = "/health" },
            Deployment = new ServiceDeploymentSpecification(),
            Contracts = new ServiceContractReferences()
        };

        return new LastDeploymentRecord(plan, contract, manifest, ResolvedNetworkName: "aovias-network");
    }

    /// <summary>Private-per-test-file fakes, matching the established convention (Sprint M4.3's <c>EnvironmentRegistrationTests.cs</c>, Sprint M2.7's <c>FleetManagementTests.cs</c>).</summary>
    private sealed class AdmissionHarness : IDisposable
    {
        private readonly string tempDirectory = Path.Combine(Path.GetTempPath(), "aovias-m44-tests-" + Guid.NewGuid().ToString("N"));

        public AdmissionHarness()
        {
            PathResolver = new FakeDeploymentPathResolver(tempDirectory);
            AdmissionStore = new EnvironmentAdmissionStore(PathResolver, NullLogger<EnvironmentAdmissionStore>.Instance);
            PlanStore = new LastDeploymentPlanStore();
            RegistrationSettings = new StaticOptionsMonitor<EnvironmentRegistrationSettings>(
                new EnvironmentRegistrationSettings { CapabilityProfileId = "docker-host" });

            HostedService = new EnvironmentAdmissionHostedService(
                RegistrationSettings,
                PlanStore,
                AdmissionStore,
                NullLogger<EnvironmentAdmissionHostedService>.Instance);

            var runtimeSettings = new StaticOptionsMonitor<RuntimeSettings>(new RuntimeSettings
            {
                ServerId = "local",
                ServerName = "local",
                Hostname = "localhost",
                RuntimeVersion = "1.0.0",
                PlatformVersion = "1.0.0"
            });
            var capabilitiesProvider = new RuntimeCapabilitiesProvider(
                new PlatformFeatureActivation { Runtime = true },
                new StaticOptionsMonitor<SignalRSettings>(new SignalRSettings()),
                new StaticOptionsMonitor<ProviderSettings>(new ProviderSettings()));

            HeartbeatService = new RuntimeHeartbeatService(
                runtimeSettings,
                capabilitiesProvider,
                PlanStore,
                new InMemoryReconciliationHistoryStore(),
                new DeploymentReliabilityMetricsStore(),
                new MaintenanceModeStore(),
                new FakeHealthProvider(),
                AdmissionStore);
        }

        public FakeDeploymentPathResolver PathResolver { get; }

        public EnvironmentAdmissionStore AdmissionStore { get; }

        public LastDeploymentPlanStore PlanStore { get; }

        public StaticOptionsMonitor<EnvironmentRegistrationSettings> RegistrationSettings { get; }

        public EnvironmentAdmissionHostedService HostedService { get; }

        public RuntimeHeartbeatService HeartbeatService { get; }

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
        public string ConfigRoot { get; } = configRoot;

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

    private sealed class FakeHealthProvider : IHealthProvider
    {
        public Task<RuntimeHealthDto> GetHealthAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new RuntimeHealthDto { Status = "Healthy" });
    }

    private sealed class StaticOptionsMonitor<T>(T current) : IOptionsMonitor<T>
    {
        public T Current { get; set; } = current;

        public T CurrentValue => Current;

        public T Get(string? name) => Current;

        public IDisposable OnChange(Action<T, string?> listener) => NullDisposable.Instance;

        private sealed class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
