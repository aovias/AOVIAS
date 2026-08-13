using System.Collections.ObjectModel;
using System.IO;
using ComplianceServiceManager.Logging;
using ComplianceServiceManager.Models;
using ComplianceServiceManager.Models.PortRegistry;
using ComplianceServiceManager.Models.ServiceInstances;
using ComplianceServiceManager.Models.Workspaces;
using ComplianceServiceManager.Services;
using ComplianceServiceManager.Services.PortRegistry;
using ComplianceServiceManager.Services.ServiceInstances;
using ComplianceServiceManager.Services.Workspaces;

namespace ComplianceServiceManager.Tests.PortRegistry;

public sealed class PortRegistryServiceTests : IDisposable
{
    private readonly string root;
    private readonly string instanceStorePath;
    private readonly string portStorePath;
    private readonly string workspaceStore;
    private readonly FakeServiceRuntimeHost host;
    private readonly FakePortAvailability availability;
    private readonly ServiceInstanceRegistry instanceRegistry;
    private readonly ServiceInstanceService instances;
    private readonly PortRegistryStore store;
    private readonly PortRegistryService registry;
    private readonly DevelopmentWorkspaceRegistry workspaces;
    private readonly TrackingLogger logger;

    public PortRegistryServiceTests()
    {
        root = Path.Combine(Path.GetTempPath(), "aovias-port-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var main = Path.Combine(root, "AOVIAS");
        var cursor = Path.Combine(root, "AOVIAS.Cursor");
        var claude = Path.Combine(root, "AOVIAS.Claude");
        Directory.CreateDirectory(main);

        InitGit(main, "main");
        InitGitWorktree(main, cursor, "agent/cursor/organization-feature");
        InitGitWorktree(main, claude, "agent/claude/def-env-004-server-log-errors");

        instanceStorePath = Path.Combine(root, "service-instances.json");
        portStorePath = Path.Combine(root, "port-registry.json");
        workspaceStore = Path.Combine(root, "development-workspaces.json");
        File.WriteAllText(workspaceStore, System.Text.Json.JsonSerializer.Serialize(
            DevelopmentWorkspaceRegistry.BuildDefaults(root),
            new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            }));

        host = new FakeServiceRuntimeHost();
        host.Services.Add(new ServiceInfoModel
        {
            Name = "OrganizationService",
            Status = ServiceStatus.Stopped,
            HttpsPort = null,
            ProcessId = null
        });

        workspaces = new DevelopmentWorkspaceRegistry(workspaceStore);
        var gitRunner = new GitCliRunner(() => new[] { root, main, cursor, claude });
        instanceRegistry = new ServiceInstanceRegistry(instanceStorePath);
        logger = new TrackingLogger();
        instances = new ServiceInstanceService(
            instanceRegistry,
            workspaces,
            new GitWorkspaceService(gitRunner),
            host,
            logger);

        availability = new FakePortAvailability();
        store = new PortRegistryStore(portStorePath);
        registry = new PortRegistryService(
            store,
            availability,
            instances,
            instanceRegistry,
            workspaces,
            host,
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

    [Theory]
    [InlineData(1)]
    [InlineData(5101)]
    [InlineData(65535)]
    public void ValidPort_Accepted(int port)
    {
        var shared = instances.EnsureSharedInstance("OrganizationService", "OrganizationService");
        Assert.True(registry.ValidateAssignment(port, shared.InstanceId).Succeeded);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(65536)]
    public void InvalidPort_Rejected(int port)
    {
        var shared = instances.EnsureSharedInstance("OrganizationService", "OrganizationService");
        var result = registry.ValidateAssignment(port, shared.InstanceId);
        Assert.False(result.Succeeded);
        Assert.Contains("65535", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Assign_SharedInstance()
    {
        var shared = instances.EnsureSharedInstance("OrganizationService", "OrganizationService");
        var result = registry.Assign(7217, shared.InstanceId);
        Assert.True(result.Succeeded);
        await registry.RefreshAsync();
        var item = registry.GetByPort(7217);
        Assert.NotNull(item);
        Assert.Equal("Shared", item!.InstanceDisplay);
        Assert.Equal(ServiceExecutionScope.Shared, item.ExecutionScope);
        Assert.Equal(PortEffectiveStatus.AssignedAvailable, item.EffectiveStatus);
    }

    [Fact]
    public async Task Assign_WorkspaceInstance()
    {
        var cursor = instances.EnsureWorkspaceInstance("OrganizationService", "OrganizationService", "cursor");
        var result = registry.Assign(5101, cursor.InstanceId);
        Assert.True(result.Succeeded);
        await registry.RefreshAsync();
        var item = registry.GetByPort(5101);
        Assert.NotNull(item);
        Assert.Equal("Cursor", item!.InstanceDisplay);
        Assert.Equal("Cursor", item.ScopeDisplay);
        Assert.Equal("cursor", item.WorkspaceId);
    }

    [Fact]
    public void DuplicatePort_Rejected()
    {
        var shared = instances.EnsureSharedInstance("OrganizationService", "OrganizationService");
        var cursor = instances.EnsureWorkspaceInstance("OrganizationService", "OrganizationService", "cursor");
        Assert.True(registry.Assign(5101, shared.InstanceId).Succeeded);
        var second = registry.Assign(5101, cursor.InstanceId);
        Assert.False(second.Succeeded);
        Assert.Contains("already assigned", second.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(shared.InstanceId, store.GetByPort(5101)!.InstanceId);
    }

    [Fact]
    public void SameService_MultiplePorts_ThroughDifferentInstances()
    {
        var shared = instances.EnsureSharedInstance("OrganizationService", "OrganizationService");
        var cursor = instances.EnsureWorkspaceInstance("OrganizationService", "OrganizationService", "cursor");
        var claude = instances.EnsureWorkspaceInstance("OrganizationService", "OrganizationService", "claude");
        Assert.True(registry.Assign(7217, shared.InstanceId).Succeeded);
        Assert.True(registry.Assign(5101, cursor.InstanceId).Succeeded);
        Assert.True(registry.Assign(5201, claude.InstanceId).Succeeded);
        Assert.Equal(3, store.GetAll().Count);
    }

    [Fact]
    public void Release_RemovesAssignment_DoesNotStopProcess()
    {
        var shared = instances.EnsureSharedInstance("OrganizationService", "OrganizationService");
        Assert.True(registry.Assign(7217, shared.InstanceId).Succeeded);
        Assert.True(registry.Release(7217).Succeeded);
        Assert.Null(store.GetByPort(7217));
        Assert.Equal(0, host.StopCalls);
        Assert.Equal(0, host.StartCalls);
    }

    [Fact]
    public void MissingServiceInstance_Rejected()
    {
        var result = registry.Assign(5101, "missing-instance");
        Assert.False(result.Succeeded);
        Assert.Contains("does not exist", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MissingWorkspace_RejectedOnAssign()
    {
        var missing = instances.EnsureWorkspaceInstance("OrganizationService", "OrganizationService", "missing-ws");
        var result = registry.Assign(5101, missing.InstanceId);
        Assert.False(result.Succeeded);
        Assert.Contains("Workspace", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MissingWorkspace_PreservedAsUnavailable_OnReconcile()
    {
        var cursor = instances.EnsureWorkspaceInstance("OrganizationService", "OrganizationService", "cursor");
        Assert.True(registry.Assign(5101, cursor.InstanceId).Succeeded);

        var document = workspaces.GetDocument();
        foreach (var repo in document.Repositories)
        {
            repo.Workspaces.RemoveAll(w => string.Equals(w.Id, "cursor", StringComparison.OrdinalIgnoreCase));
        }

        File.WriteAllText(
            workspaceStore,
            System.Text.Json.JsonSerializer.Serialize(
                document,
                new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                }));

        var removedWorkspaces = new DevelopmentWorkspaceRegistry(workspaceStore);
        Assert.DoesNotContain(
            removedWorkspaces.GetDocument().Repositories.SelectMany(r => r.Workspaces),
            w => string.Equals(w.Id, "cursor", StringComparison.OrdinalIgnoreCase));

        var reconciled = new PortRegistryService(
            store,
            availability,
            instances,
            instanceRegistry,
            removedWorkspaces,
            host,
            logger);
        await reconciled.RefreshAsync();
        var item = reconciled.GetByPort(5101);
        Assert.NotNull(item);
        Assert.Equal(PortEffectiveStatus.AssignedUnavailable, item!.EffectiveStatus);
        Assert.Contains("Workspace unavailable", item.AttentionReason ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(store.GetByPort(5101));
    }

    [Fact]
    public void PortAvailable_Observation()
    {
        availability.Set(61001, listening: false);
        var observation = registry.ObservePort(61001);
        Assert.False(observation.IsListening);
        Assert.Contains("Available", observation.AvailabilityDisplay, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PortOccupied_Observation()
    {
        availability.Set(61002, listening: true, pid: 18244, name: "other.exe");
        var observation = registry.ObservePort(61002);
        Assert.True(observation.IsListening);
        Assert.Equal(18244, observation.ProcessId);
        Assert.Equal("other.exe", observation.ProcessName);
    }

    [Fact]
    public async Task Occupied_ProcessDetailsUnavailable_Handled()
    {
        var shared = instances.EnsureSharedInstance("OrganizationService", "OrganizationService");
        Assert.True(registry.Assign(61003, shared.InstanceId).Succeeded);
        availability.Set(61003, listening: true, pid: null, name: null);
        await registry.RefreshAsync();
        var item = registry.GetByPort(61003);
        Assert.NotNull(item);
        Assert.Equal(PortEffectiveStatus.AssignedConflict, item!.EffectiveStatus);
        Assert.Equal("PID Unknown", item.OccupantDisplay);
    }

    [Fact]
    public async Task Occupied_UnrelatedProcess_IsConflict()
    {
        var shared = instances.EnsureSharedInstance("OrganizationService", "OrganizationService");
        Assert.True(registry.Assign(61004, shared.InstanceId).Succeeded);
        availability.Set(61004, listening: true, pid: 99999, name: "unrelated");
        await registry.RefreshAsync();
        var item = registry.GetByPort(61004);
        Assert.Equal(PortEffectiveStatus.AssignedConflict, item!.EffectiveStatus);
    }

    [Fact]
    public async Task Occupied_ExpectedProcess_WhenAuthoritative()
    {
        host.Services[0].Status = ServiceStatus.Running;
        host.Services[0].ProcessId = 4242;
        var shared = instances.EnsureSharedInstance("OrganizationService", "OrganizationService");
        Assert.True(registry.Assign(7217, shared.InstanceId).Succeeded);
        availability.Set(7217, listening: true, pid: 4242, name: "OrganizationService");
        await registry.RefreshAsync();
        var item = registry.GetByPort(7217);
        Assert.Equal(PortEffectiveStatus.AssignedOccupied, item!.EffectiveStatus);
        Assert.Contains("expected", item.AttentionReason ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Persistence_RoundTrip()
    {
        var shared = instances.EnsureSharedInstance("OrganizationService", "OrganizationService");
        Assert.True(registry.Assign(7217, shared.InstanceId).Succeeded);
        var reloaded = new PortRegistryStore(portStorePath);
        var loaded = reloaded.GetByPort(7217);
        Assert.NotNull(loaded);
        Assert.Equal(shared.InstanceId, loaded!.InstanceId);
        Assert.Equal("OrganizationService", loaded.ServiceId);
    }

    [Fact]
    public void CorruptPersistence_HandledSafely()
    {
        File.WriteAllText(portStorePath, "{ not valid json");
        var recovered = new PortRegistryStore(portStorePath);
        Assert.Empty(recovered.GetAll());
    }

    [Fact]
    public void MultipleAllocations_Persist()
    {
        var shared = instances.EnsureSharedInstance("OrganizationService", "OrganizationService");
        var cursor = instances.EnsureWorkspaceInstance("OrganizationService", "OrganizationService", "cursor");
        var claude = instances.EnsureWorkspaceInstance("OrganizationService", "OrganizationService", "claude");
        Assert.True(registry.Assign(7217, shared.InstanceId).Succeeded);
        Assert.True(registry.Assign(5101, cursor.InstanceId).Succeeded);
        Assert.True(registry.Assign(5201, claude.InstanceId).Succeeded);
        var reloaded = new PortRegistryStore(portStorePath).GetAll();
        Assert.Equal(3, reloaded.Count);
        Assert.Contains(reloaded, a => a.Port == 7217 && a.WorkspaceId is null);
        Assert.Contains(reloaded, a => a.Port == 5101 && a.WorkspaceId == "cursor");
        Assert.Contains(reloaded, a => a.Port == 5201 && a.WorkspaceId == "claude");
    }

    [Fact]
    public async Task SharedCursorClaude_Coexist_WithWorkspaceAssociation()
    {
        var shared = instances.EnsureSharedInstance("OrganizationService", "OrganizationService");
        var cursor = instances.EnsureWorkspaceInstance("OrganizationService", "OrganizationService", "cursor");
        var claude = instances.EnsureWorkspaceInstance("OrganizationService", "OrganizationService", "claude");
        Assert.True(registry.Assign(7217, shared.InstanceId).Succeeded);
        Assert.True(registry.Assign(5101, cursor.InstanceId).Succeeded);
        Assert.True(registry.Assign(5201, claude.InstanceId).Succeeded);
        await registry.RefreshAsync();
        Assert.Equal("Shared", registry.GetByPort(7217)!.InstanceDisplay);
        Assert.Equal("Cursor", registry.GetByPort(5101)!.InstanceDisplay);
        Assert.Equal("Claude", registry.GetByPort(5201)!.InstanceDisplay);
        Assert.Equal("cursor", registry.GetByPort(5101)!.WorkspaceId);
        Assert.Equal("claude", registry.GetByPort(5201)!.WorkspaceId);
    }

    [Fact]
    public async Task AgentAndRuntimeProfile_RemainAvailableThroughServiceInstance()
    {
        var cursor = instances.EnsureWorkspaceInstance(
            "OrganizationService",
            "OrganizationService",
            "cursor",
            agentSessionId: "sess-1",
            runtimeProfileId: "profile-1");
        Assert.True(registry.Assign(5101, cursor.InstanceId).Succeeded);
        await instances.RefreshAsync();
        await registry.RefreshAsync();
        var live = instances.GetInstances().Single(i => i.InstanceId == cursor.InstanceId);
        Assert.Equal("sess-1", live.AgentSessionId);
        Assert.Equal("profile-1", live.RuntimeProfileId);
        Assert.Equal(cursor.InstanceId, registry.GetByPort(5101)!.InstanceId);
    }

    [Fact]
    public async Task ConcurrentDuplicateAssignment_Rejected()
    {
        var shared = instances.EnsureSharedInstance("OrganizationService", "OrganizationService");
        var cursor = instances.EnsureWorkspaceInstance("OrganizationService", "OrganizationService", "cursor");
        var results = new PortRegistryResult[2];
        var t1 = Task.Run(() => results[0] = registry.Assign(5301, shared.InstanceId));
        var t2 = Task.Run(() => results[1] = registry.Assign(5301, cursor.InstanceId));
        await Task.WhenAll(t1, t2);
        Assert.Equal(1, results.Count(r => r.Succeeded));
        Assert.Equal(1, results.Count(r => !r.Succeeded));
        Assert.Single(store.GetAll(), a => a.Port == 5301);
    }

    [Fact]
    public async Task Registry_DoesNotStartOrStopServices()
    {
        var shared = instances.EnsureSharedInstance("OrganizationService", "OrganizationService");
        Assert.True(registry.Assign(5401, shared.InstanceId).Succeeded);
        availability.Set(5401, listening: true, pid: 1, name: "x");
        await registry.RefreshAsync();
        Assert.True(registry.Release(5401).Succeeded);
        Assert.Equal(0, host.StartCalls);
        Assert.Equal(0, host.StopCalls);
        Assert.DoesNotContain(logger.Messages, e => e.Contains("kill", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void FindFirstAvailablePort_IsReadOnly()
    {
        availability.Set(60001, listening: true);
        availability.Set(60002, listening: false);
        var found = availability.FindFirstAvailablePort(60001, 60005, excludedPorts: [60003]);
        Assert.Equal(60002, found);
        Assert.Empty(store.GetAll());
    }

    private static void InitGit(string path, string branch)
    {
        RunGit(path, "init");
        RunGit(path, "config user.email test@example.com");
        RunGit(path, "config user.name Test");
        File.WriteAllText(Path.Combine(path, "README.md"), "main\n");
        RunGit(path, "add README.md");
        RunGit(path, "commit -m init");
        RunGit(path, $"branch -M {branch}");
    }

    private static void InitGitWorktree(string main, string worktree, string branch)
    {
        RunGit(main, $"worktree add \"{worktree}\" -b {branch}");
    }

    private static void RunGit(string cwd, string args)
    {
        var psi = new System.Diagnostics.ProcessStartInfo("git", args)
        {
            WorkingDirectory = cwd,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var process = System.Diagnostics.Process.Start(psi)!;
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(process.StandardError.ReadToEnd());
        }
    }

    private sealed class FakePortAvailability : IPortAvailabilityService
    {
        private readonly Dictionary<int, PortOccupancyObservation> map = new();

        public void Set(int port, bool listening, int? pid = null, string? name = null, string? error = null)
        {
            map[port] = new PortOccupancyObservation
            {
                Port = port,
                IsListening = listening,
                ProcessId = pid,
                ProcessName = name,
                Error = error
            };
        }

        public PortOccupancyObservation Observe(int port)
        {
            if (map.TryGetValue(port, out var observation))
            {
                return observation;
            }

            return new PortOccupancyObservation { Port = port, IsListening = false };
        }

        public int? FindFirstAvailablePort(int startInclusive, int endInclusive, IEnumerable<int>? excludedPorts = null)
        {
            var excluded = new HashSet<int>(excludedPorts ?? []);
            for (var port = startInclusive; port <= endInclusive; port++)
            {
                if (excluded.Contains(port))
                {
                    continue;
                }

                var observation = Observe(port);
                if (!observation.IsListening && string.IsNullOrWhiteSpace(observation.Error))
                {
                    return port;
                }
            }

            return null;
        }
    }

    private sealed class FakeServiceRuntimeHost : IServiceRuntimeHost
    {
        public ObservableCollection<ServiceInfoModel> Services { get; } = [];
        public int StartCalls { get; private set; }
        public int StopCalls { get; private set; }

        public Task StartAsync(ServiceInfoModel service, CancellationToken cancellationToken)
        {
            StartCalls++;
            return Task.CompletedTask;
        }

        public Task StopAsync(ServiceInfoModel service, CancellationToken cancellationToken)
        {
            StopCalls++;
            return Task.CompletedTask;
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
