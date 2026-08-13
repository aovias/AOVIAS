using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using ComplianceServiceManager.Logging;
using ComplianceServiceManager.Models;
using ComplianceServiceManager.Models.PortRegistry;
using ComplianceServiceManager.Models.RuntimeProfiles;
using ComplianceServiceManager.Models.ServiceInstances;
using ComplianceServiceManager.Models.SharedRuntime;
using ComplianceServiceManager.Models.Workspaces;
using ComplianceServiceManager.Services;
using ComplianceServiceManager.Services.PortRegistry;
using ComplianceServiceManager.Services.RuntimeProfiles;
using ComplianceServiceManager.Services.ServiceInstances;
using ComplianceServiceManager.Services.SharedRuntime;
using ComplianceServiceManager.Services.Workspaces;

namespace ComplianceServiceManager.Tests.SharedRuntime;

public sealed class SharedRuntimeServiceTests : IDisposable
{
    private readonly string root;
    private readonly string mainPath;
    private readonly string cursorPath;
    private readonly string claudePath;
    private readonly FakeServiceRuntimeHost host;
    private readonly FakeInstances instances;
    private readonly FakePorts ports;
    private readonly FakeAvailability availability;
    private readonly FakeHealthProbe health;
    private readonly RuntimeProfileOrchestrator orchestrator;
    private readonly SharedRuntimeService runtime;
    private readonly TrackingLogger logger;
    private readonly string profileStore;

    public SharedRuntimeServiceTests()
    {
        root = Path.Combine(Path.GetTempPath(), "aovias-sr-" + Guid.NewGuid().ToString("N"));
        mainPath = Path.Combine(root, "AOVIAS");
        cursorPath = Path.Combine(root, "AOVIAS.Cursor");
        claudePath = Path.Combine(root, "AOVIAS.Claude");
        Directory.CreateDirectory(mainPath);
        Directory.CreateDirectory(cursorPath);
        Directory.CreateDirectory(claudePath);

        var workspaceStore = Path.Combine(root, "development-workspaces.json");
        File.WriteAllText(workspaceStore, JsonSerializer.Serialize(
            DevelopmentWorkspaceRegistry.BuildDefaults(root),
            new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter() }
            }));

        host = new FakeServiceRuntimeHost();
        instances = new FakeInstances();
        ports = new FakePorts();
        availability = new FakeAvailability();
        health = new FakeHealthProbe();
        logger = new TrackingLogger();
        profileStore = Path.Combine(root, "runtime-profiles.json");
        orchestrator = new RuntimeProfileOrchestrator(
            new RuntimeProfileRegistry(profileStore),
            new RuntimeProfileValidator(),
            host,
            health,
            logger);
        orchestrator.ConfigureHealthWaitForTests(TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(20));

        runtime = new SharedRuntimeService(
            orchestrator,
            host,
            instances,
            ports,
            availability,
            new DevelopmentWorkspaceRegistry(workspaceStore),
            logger);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(root, recursive: true);
        }
        catch
        {
        }
    }

    [Fact]
    public void Plan_ValidSharedRuntime()
    {
        var profile = SeedWebStack();
        var plan = runtime.Plan(profile);
        Assert.True(plan.CanStart);
        Assert.Equal(5, plan.ServiceCount);
        Assert.Equal(5, plan.ToStartCount);
        Assert.Equal("Shared", plan.ScopeDisplay);
        Assert.Contains(mainPath, plan.CanonicalSource, StringComparison.OrdinalIgnoreCase);
        Assert.All(plan.Entries, e => Assert.Equal(SharedRuntimeAction.Start, e.Action));
        Assert.Empty(host.StartCalls);
    }

    [Fact]
    public void Plan_InvalidProfile_Blocked()
    {
        var profile = Profile(("WebApp", ["Missing"]));
        AddService("WebApp");
        var plan = runtime.Plan(profile);
        Assert.False(plan.CanStart);
        Assert.Contains(plan.Issues, i => i.Code is "missing-dependency" or "unknown-service");
        Assert.Empty(host.StartCalls);
    }

    [Fact]
    public void Plan_MissingService_Blocked()
    {
        var profile = Profile(("Gateway", []));
        var plan = runtime.Plan(profile);
        Assert.False(plan.CanStart);
        Assert.Contains(plan.Issues, i => i.Code is "discovery-empty" or "unknown-service" or "missing-service");
        Assert.Equal(plan.Issues.Count, plan.IssueCount);
        Assert.Contains($"{plan.IssueCount} issue(s)", plan.Summary);
        Assert.Equal(0, plan.BlockedCount);
    }

    [Fact]
    public void Plan_FiveUnknownServices_IssueCountIsFive()
    {
        AddService("UnrelatedService");
        var profile = orchestrator.GetProfiles()
            .First(p => p.Name.Contains("Web", StringComparison.OrdinalIgnoreCase));
        var plan = runtime.Plan(profile);
        Assert.False(plan.CanStart);
        Assert.Equal(5, plan.IssueCount);
        Assert.Equal(5, plan.Issues.Count);
        Assert.Equal(SharedRuntimeStatus.Blocked, runtime.Session.Status);
        Assert.Contains("5 issue(s)", plan.Summary);
        Assert.Equal(0, plan.BlockedCount);
        Assert.All(plan.Issues, i => Assert.Equal("unknown-service", i.Code));
        Assert.Empty(host.StartCalls);
    }

    [Fact]
    public void Plan_EmptyDiscovery_DoesNotReportFiveUnknownServices()
    {
        var profile = orchestrator.GetProfiles()
            .First(p => p.Name.Contains("Web", StringComparison.OrdinalIgnoreCase));
        var plan = runtime.Plan(profile);
        Assert.False(plan.CanStart);
        Assert.Equal(1, plan.IssueCount);
        Assert.Contains(plan.Issues, i => i.Code == "discovery-empty");
        Assert.Contains("1 issue(s)", plan.Summary);
        Assert.Equal(SharedRuntimeStatus.Blocked, runtime.Session.Status);
    }

    [Fact]
    public void Plan_ValidProfile_CanStartRemainsTrue()
    {
        var profile = SeedWebStack();
        var plan = runtime.Plan(profile);
        Assert.True(plan.CanStart);
        Assert.Equal(0, plan.IssueCount);
        Assert.Equal(SharedRuntimeStatus.Planned, runtime.Session.Status);
        Assert.StartsWith("VALID", plan.Summary, StringComparison.Ordinal);
    }

    [Fact]
    public void Plan_MissingSharedInstance_Blocked()
    {
        AddDiscoveredOnly("Gateway");
        var profile = Profile(("Gateway", []));
        var plan = runtime.Plan(profile);
        Assert.False(plan.CanStart);
        Assert.Contains(plan.Issues, i => i.Code == "missing-instance");
    }

    [Fact]
    public void ValidateInstance_WorkspaceRejected()
    {
        var item = WorkspaceInstance("OrganizationService", "other");
        var issue = runtime.ValidateInstanceForSharedRuntime(item);
        Assert.NotNull(issue);
        Assert.Equal("workspace-instance", issue!.Code);
    }

    [Fact]
    public void ValidateInstance_CursorRejected()
    {
        var issue = runtime.ValidateInstanceForSharedRuntime(WorkspaceInstance("OrganizationService", "cursor"));
        Assert.Contains("Cursor", issue!.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateInstance_ClaudeRejected()
    {
        var issue = runtime.ValidateInstanceForSharedRuntime(WorkspaceInstance("OrganizationService", "claude"));
        Assert.Contains("Claude", issue!.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateInstance_SharedAccepted()
    {
        Assert.Null(runtime.ValidateInstanceForSharedRuntime(SharedInstance("Gateway")));
    }

    [Fact]
    public void Plan_InvalidPort_Blocked()
    {
        AddService("Gateway", port: null);
        var profile = Profile(("Gateway", []));
        var plan = runtime.Plan(profile);
        Assert.False(plan.CanStart);
        Assert.Contains(plan.Issues, i => i.Code is "missing-port" or "missing-health");
    }

    [Fact]
    public void Plan_PortConflict_Blocked()
    {
        AddService("Gateway", port: 7217);
        availability.Set(7217, listening: true, pid: 12345, name: "unrelated");
        var profile = Profile(("Gateway", []));
        var plan = runtime.Plan(profile);
        Assert.False(plan.CanStart);
        Assert.Contains(plan.Issues, i => i.Code == "port-conflict");
        Assert.Empty(host.StartCalls);
    }

    [Fact]
    public void Plan_PortRegistryConflict_Blocked()
    {
        var gateway = AddService("Gateway", port: 7217);
        ports.ByPort[7217] = new PortAllocationItem { Port = 7217, InstanceId = "someone-else", ServiceId = "Other" };
        ports.ByInstance[gateway.InstanceId] = new PortAllocationItem { Port = 7217, InstanceId = gateway.InstanceId };
        var profile = Profile(("Gateway", []));
        var plan = runtime.Plan(profile);
        Assert.False(plan.CanStart);
        Assert.Contains(plan.Issues, i => i.Code == "port-registry-conflict");
    }

    [Fact]
    public void Plan_DependencyOrder_FromD2()
    {
        var profile = SeedWebStack();
        var plan = runtime.Plan(profile);
        Assert.Equal(
            ["IdentityService", "OrganizationService", "ComplianceService", "Gateway", "WebApp"],
            plan.StartupOrder.ToArray());
        Assert.True(IndexOf(plan, "OrganizationService") < IndexOf(plan, "ComplianceService"));
        Assert.True(IndexOf(plan, "Gateway") < IndexOf(plan, "WebApp"));
    }

    [Fact]
    public void Plan_DependencyCycle_Blocked()
    {
        AddService("A");
        AddService("B");
        var profile = Profile(("A", ["B"]), ("B", ["A"]));
        var plan = runtime.Plan(profile);
        Assert.False(plan.CanStart);
        Assert.Contains(plan.Issues, i => i.Code == "circular-dependency");
    }

    [Fact]
    public async Task Start_ReusesHealthyRunning_DoesNotClaimExternal()
    {
        AddService("IdentityService", port: 5100, status: ServiceStatus.Running, healthStatus: HealthStatus.Healthy, pid: 42);
        AddService("Gateway", port: 7200);
        var profile = Profile(("IdentityService", []), ("Gateway", ["IdentityService"]));
        var plan = runtime.Plan(profile);
        Assert.Equal(SharedRuntimeAction.Reuse, plan.Entries.Single(e => e.ServiceName == "IdentityService").Action);
        Assert.Equal("External", plan.Entries.Single(e => e.ServiceName == "IdentityService").OwnerDisplay);

        var result = await runtime.StartAsync(profile);
        Assert.True(result.Succeeded);
        Assert.DoesNotContain("IdentityService", profile.OwnedServiceNames);
        Assert.Contains("Gateway", profile.OwnedServiceNames);
        Assert.Equal(["Gateway"], host.StartCalls);
    }

    [Fact]
    public void Plan_UnhealthyRunning_Blocked()
    {
        AddService("Gateway", port: 7200, status: ServiceStatus.Running, healthStatus: HealthStatus.Unreachable, pid: 9);
        var profile = Profile(("Gateway", []));
        var plan = runtime.Plan(profile);
        Assert.False(plan.CanStart);
        Assert.Contains(plan.Issues, i => i.Code == "unhealthy-running");
        Assert.Empty(host.StartCalls);
    }

    [Fact]
    public async Task Start_Success_AssignsOwnership()
    {
        AddService("Gateway", port: 7200);
        var profile = Profile(("Gateway", []));
        var result = await runtime.StartAsync(profile);
        Assert.True(result.Succeeded);
        Assert.Contains("Gateway", profile.OwnedServiceNames);
        Assert.Equal(["Gateway"], host.StartCalls);
        Assert.Equal(SharedRuntimeStatus.Running, result.Status);
    }

    [Fact]
    public async Task Start_ProcessFailure_BlocksDependent()
    {
        host.FailOnStart.Add("OrganizationService");
        AddService("OrganizationService", port: 7217);
        AddService("ComplianceService", port: 7218);
        var profile = Profile(
            ("OrganizationService", []),
            ("ComplianceService", ["OrganizationService"]));
        var result = await runtime.StartAsync(profile);
        Assert.False(result.Succeeded);
        Assert.Equal(RuntimeProfileMemberStatus.Failed, profile.Members.Single(m => m.ServiceName == "OrganizationService").Status);
        Assert.Equal(RuntimeProfileMemberStatus.Blocked, profile.Members.Single(m => m.ServiceName == "ComplianceService").Status);
        Assert.DoesNotContain("ComplianceService", host.StartCalls);
    }

    [Fact]
    public async Task Start_HealthTimeout_FailsMember()
    {
        health.AlwaysUnreachable = true;
        AddService("Gateway", port: 7200);
        var profile = Profile(("Gateway", []));
        var result = await runtime.StartAsync(profile);
        Assert.False(result.Succeeded);
        Assert.Equal(RuntimeProfileMemberStatus.Failed, profile.Members.Single().Status);
        Assert.Contains("Gateway", profile.OwnedServiceNames);
    }

    [Fact]
    public async Task Start_HealthSuccess()
    {
        AddService("Gateway", port: 7200);
        var profile = Profile(("Gateway", []));
        var result = await runtime.StartAsync(profile);
        Assert.True(result.Succeeded);
        Assert.Equal(RuntimeProfileMemberStatus.Ready, profile.Members.Single().Status);
    }

    [Fact]
    public async Task Stop_OnlyOwned_ExternalRemains()
    {
        AddService("IdentityService", port: 5100, status: ServiceStatus.Running, healthStatus: HealthStatus.Healthy, pid: 1);
        AddService("Gateway", port: 7200);
        var profile = Profile(("IdentityService", []), ("Gateway", ["IdentityService"]));
        await runtime.StartAsync(profile);
        host.StopCalls.Clear();
        await runtime.StopAsync(profile);
        Assert.Equal(["Gateway"], host.StopCalls);
        Assert.Empty(profile.OwnedServiceNames);
        Assert.Equal(ServiceStatus.Running, host.Services.Single(s => s.Name == "IdentityService").Status);
    }

    [Fact]
    public async Task Start_BlockedPlan_DoesNotStart()
    {
        AddService("Gateway", port: 7200);
        availability.Set(7200, listening: true, pid: 99, name: "other");
        var profile = Profile(("Gateway", []));
        var result = await runtime.StartAsync(profile);
        Assert.True(result.Blocked);
        Assert.Empty(host.StartCalls);
        Assert.Empty(profile.OwnedServiceNames);
    }

    [Fact]
    public async Task Start_Duplicate_Blocked()
    {
        host.StartDelay = TimeSpan.FromMilliseconds(400);
        AddService("Gateway", port: 7200);
        var profile = Profile(("Gateway", []));
        var first = runtime.StartAsync(profile);
        await Task.Delay(30);
        var second = await runtime.StartAsync(profile);
        Assert.True(second.Blocked);
        Assert.Contains("already in progress", second.Error, StringComparison.OrdinalIgnoreCase);
        await first;
    }

    [Fact]
    public async Task Start_Cancellation_PreservesStarted()
    {
        host.StartDelay = TimeSpan.FromMilliseconds(800);
        AddService("Gateway", port: 7200);
        var profile = Profile(("Gateway", []));
        var cts = new CancellationTokenSource();
        var task = runtime.StartAsync(profile, cts.Token);
        await Task.Delay(40);
        runtime.CancelStartup();
        cts.Cancel();
        var result = await task;
        Assert.False(result.Succeeded);
        Assert.Contains("cancelled", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Stop_Idempotent()
    {
        AddService("Gateway", port: 7200);
        var profile = Profile(("Gateway", []));
        await runtime.StartAsync(profile);
        await runtime.StopAsync(profile);
        host.StopCalls.Clear();
        var second = await runtime.StopAsync(profile);
        Assert.True(second.Succeeded);
        Assert.Empty(host.StopCalls);
    }

    [Fact]
    public void Plan_DoesNotMutatePortsGitOrSessions()
    {
        SeedWebStack();
        var profile = orchestrator.GetProfiles().First(p => p.Name.Contains("Web", StringComparison.OrdinalIgnoreCase))
                      ?? Profile(("IdentityService", []), ("OrganizationService", []), ("ComplianceService", ["OrganizationService"]), ("Gateway", ["IdentityService", "ComplianceService"]), ("WebApp", ["Gateway"]));
        _ = runtime.Plan(profile);
        Assert.Equal(0, ports.AssignCalls);
        Assert.Equal(0, ports.ReleaseCalls);
        Assert.Empty(host.StartCalls);
        Assert.Empty(host.StopCalls);
        Assert.DoesNotContain(logger.Messages, m => m.Contains("git ", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Plan_RejectsCursorSourcePath()
    {
        var path = Path.Combine(cursorPath, "Gateway");
        Directory.CreateDirectory(path);
        var item = SharedInstance("Gateway");
        instances.Items.Add(item);
        host.Services.Add(new ServiceInfoModel
        {
            Name = "Gateway",
            ProjectPath = path,
            HttpsPort = 7200
        });
        var profile = Profile(("Gateway", []));
        var plan = runtime.Plan(profile);
        Assert.False(plan.CanStart);
        Assert.Contains(plan.Issues, i => i.Code == "non-canonical-source");
    }

    [Fact]
    public void Plan_DoesNotAutoSwitchProfileOrReassignPorts()
    {
        SeedWebStack();
        var before = orchestrator.GetProfiles().Select(p => p.Id).ToList();
        _ = runtime.Plan(Profile(("Gateway", [])));
        Assert.Equal(before, orchestrator.GetProfiles().Select(p => p.Id).ToList());
        Assert.Equal(0, ports.AssignCalls);
    }

    [Fact]
    public async Task RuntimeStatus_ReconcilesAfterStartAndStop()
    {
        AddService("Gateway", port: 7200);
        var profile = Profile(("Gateway", []));
        await runtime.StartAsync(profile);
        Assert.Equal(SharedRuntimeStatus.Running, runtime.Session.Status);
        await runtime.StopAsync(profile);
        Assert.Equal(SharedRuntimeStatus.Stopped, runtime.Session.Status);
    }

    [Fact]
    public void SharedRuntime_DoesNotTakeTelemetryOrAgentDependencies()
    {
        var ctor = typeof(SharedRuntimeService).GetConstructors().Single();
        var types = ctor.GetParameters().Select(p => p.ParameterType).ToArray();
        Assert.DoesNotContain(types, t => t.Name.Contains("LocalResource", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(types, t => t.Name.Contains("ResourceIntelligence", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(types, t => t.Name.Contains("AgentSession", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(types, t => t.Name.Contains("Git", StringComparison.OrdinalIgnoreCase));
    }

    private RuntimeProfileItem SeedWebStack()
    {
        AddService("IdentityService", port: 5100);
        AddService("OrganizationService", port: 7217);
        AddService("ComplianceService", port: 7218);
        AddService("Gateway", port: 7200);
        AddService("WebApp", port: 4200, kind: ServiceKind.AngularWebApp);
        return Profile(
            ("IdentityService", []),
            ("OrganizationService", []),
            ("ComplianceService", ["OrganizationService"]),
            ("Gateway", ["IdentityService", "ComplianceService"]),
            ("WebApp", ["Gateway"]));
    }

    private SharedInstanceAddResult AddService(
        string name,
        int? port = 7000,
        ServiceStatus status = ServiceStatus.Stopped,
        HealthStatus healthStatus = HealthStatus.Unknown,
        int? pid = null,
        ServiceKind kind = ServiceKind.WebApi)
    {
        var path = Path.Combine(mainPath, name);
        Directory.CreateDirectory(path);
        var instance = SharedInstance(name);
        instances.Items.Add(instance);
        if (port is int assigned)
        {
            ports.ByInstance[instance.InstanceId] = new PortAllocationItem
            {
                Port = assigned,
                InstanceId = instance.InstanceId,
                ServiceId = name
            };
        }

        host.Services.Add(new ServiceInfoModel
        {
            Name = name,
            ProjectPath = path,
            HttpsPort = kind == ServiceKind.WebApi ? port : null,
            HttpPort = kind == ServiceKind.AngularWebApp ? port : null,
            LaunchUrl = kind == ServiceKind.AngularWebApp ? $"http://localhost:{port ?? 4200}" : null,
            Status = status,
            Health = healthStatus,
            ProcessId = pid,
            Kind = kind
        });
        return new SharedInstanceAddResult(instance.InstanceId);
    }

    private void AddDiscoveredOnly(string name)
    {
        var path = Path.Combine(mainPath, name);
        Directory.CreateDirectory(path);
        host.Services.Add(new ServiceInfoModel { Name = name, ProjectPath = path, HttpsPort = 7200 });
    }

    private static ServiceInstanceItem SharedInstance(string service)
        => new()
        {
            InstanceId = Guid.NewGuid().ToString("N"),
            ServiceId = service,
            ServiceName = service,
            ExecutionScope = ServiceExecutionScope.Shared,
            Ownership = ServiceInstanceOwnership.Shared,
            IsConfigured = true
        };

    private static ServiceInstanceItem WorkspaceInstance(string service, string workspaceId)
        => new()
        {
            InstanceId = Guid.NewGuid().ToString("N"),
            ServiceId = service,
            ServiceName = service,
            ExecutionScope = ServiceExecutionScope.Workspace,
            Ownership = ServiceInstanceOwnership.Workspace,
            WorkspaceId = workspaceId,
            IsConfigured = true
        };

    private static RuntimeProfileItem Profile(params (string Name, string[] DependsOn)[] services)
        => RuntimeProfileItem.FromDefinition(new RuntimeProfileDefinition
        {
            Id = "test-profile",
            Name = "Test Shared",
            Enabled = true,
            MaxParallelStarts = 2,
            Services = services.Select(item => new RuntimeProfileServiceEntry
            {
                ServiceName = item.Name,
                Enabled = true,
                DependsOn = item.DependsOn.ToList()
            }).ToList()
        });

    private static int IndexOf(SharedRuntimePlan plan, string name)
        => plan.StartupOrder
            .Select((value, index) => (value, index))
            .First(pair => string.Equals(pair.value, name, StringComparison.OrdinalIgnoreCase))
            .index;

    private sealed record SharedInstanceAddResult(string InstanceId);

    private sealed class FakeInstances : IServiceInstanceService
    {
        public List<ServiceInstanceItem> Items { get; } = [];

        public event Action? InstancesChanged;

        public IReadOnlyList<ServiceInstanceItem> GetInstances() => Items;

        public IReadOnlyList<ServiceInstanceItem> GetInstancesForService(string serviceId)
            => Items.Where(i => string.Equals(i.ServiceId, serviceId, StringComparison.OrdinalIgnoreCase)).ToList();

        public Task RefreshAsync(CancellationToken cancellationToken = default)
        {
            InstancesChanged?.Invoke();
            return Task.CompletedTask;
        }

        public ServiceInstanceItem EnsureSharedInstance(string serviceId, string serviceName)
            => Items.First(i => i.ServiceId == serviceId && i.ExecutionScope == ServiceExecutionScope.Shared);

        public ServiceInstanceItem EnsureWorkspaceInstance(
            string serviceId,
            string serviceName,
            string workspaceId,
            string? agentSessionId = null,
            string? runtimeProfileId = null)
            => Items.First(i => i.WorkspaceId == workspaceId);

        public IReadOnlyList<ServiceInstanceItem> GetNotConfiguredPlaceholders(string serviceId, string serviceName)
            => [];
    }

    private sealed class FakePorts : IPortRegistryService
    {
        public Dictionary<string, PortAllocationItem> ByInstance { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<int, PortAllocationItem> ByPort { get; } = [];
        public int AssignCalls { get; private set; }
        public int ReleaseCalls { get; private set; }

        public event Action? AllocationsChanged;

        public IReadOnlyList<PortAllocationItem> GetAllocations() => ByInstance.Values.ToList();

        public PortAllocationItem? GetByPort(int port) => ByPort.TryGetValue(port, out var item) ? item : null;

        public PortAllocationItem? GetByInstanceId(string instanceId)
            => ByInstance.TryGetValue(instanceId, out var item) ? item : null;

        public Task RefreshAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public PortRegistryResult ValidateAssignment(int port, string instanceId) => PortRegistryResult.Ok();

        public PortRegistryResult Assign(int port, string instanceId)
        {
            AssignCalls++;
            return PortRegistryResult.Ok();
        }

        public PortRegistryResult Release(int port)
        {
            ReleaseCalls++;
            return PortRegistryResult.Ok();
        }

        public PortOccupancyObservation ObservePort(int port) => new() { Port = port, IsListening = false };
    }

    private sealed class FakeAvailability : IPortAvailabilityService
    {
        private readonly Dictionary<int, PortOccupancyObservation> map = [];

        public void Set(int port, bool listening, int? pid = null, string? name = null)
            => map[port] = new PortOccupancyObservation
            {
                Port = port,
                IsListening = listening,
                ProcessId = pid,
                ProcessName = name
            };

        public PortOccupancyObservation Observe(int port)
            => map.TryGetValue(port, out var observation)
                ? observation
                : new PortOccupancyObservation { Port = port, IsListening = false };

        public int? FindFirstAvailablePort(int startInclusive, int endInclusive, IEnumerable<int>? excludedPorts = null)
            => null;
    }

    private sealed class FakeServiceRuntimeHost : IServiceRuntimeHost
    {
        public ObservableCollection<ServiceInfoModel> Services { get; } = [];
        public List<string> StartCalls { get; } = [];
        public List<string> StopCalls { get; } = [];
        public HashSet<string> FailOnStart { get; } = new(StringComparer.OrdinalIgnoreCase);
        public TimeSpan StartDelay { get; set; }

        public async Task StartAsync(ServiceInfoModel service, CancellationToken cancellationToken)
        {
            if (StartDelay > TimeSpan.Zero)
            {
                await Task.Delay(StartDelay, cancellationToken);
            }

            StartCalls.Add(service.Name);
            if (FailOnStart.Contains(service.Name))
            {
                service.Status = ServiceStatus.Error;
                service.LastAction = "Simulated start failure";
                return;
            }

            service.Status = ServiceStatus.Running;
            service.Health = HealthStatus.Healthy;
            service.LastAction = "Started";
        }

        public Task StopAsync(ServiceInfoModel service, CancellationToken cancellationToken)
        {
            StopCalls.Add(service.Name);
            service.Status = ServiceStatus.Stopped;
            service.LastAction = "Stopped";
            return Task.CompletedTask;
        }
    }

    private sealed class FakeHealthProbe : IHealthProbe
    {
        public bool AlwaysUnreachable { get; set; }

        public Task<HealthStatus> CheckAsync(ServiceInfoModel service, CancellationToken cancellationToken)
        {
            if (AlwaysUnreachable)
            {
                return Task.FromResult(HealthStatus.Unreachable);
            }

            return Task.FromResult(service.Status == ServiceStatus.Running ? HealthStatus.Healthy : HealthStatus.Unknown);
        }
    }

    private sealed class TrackingLogger : ILoggingService
    {
        public ObservableCollection<LogEntryModel> Entries { get; } = [];
        public List<string> Messages { get; } = [];

        public void Log(LogLevel level, string source, string message) => Messages.Add(message);
        public void Info(string source, string message) => Messages.Add(message);
        public void Warning(string source, string message) => Messages.Add(message);
        public void Error(string source, string message) => Messages.Add(message);
        public void Build(string source, string message) => Messages.Add(message);
        public void Process(string source, string message) => Messages.Add(message);
    }
}
