using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using ComplianceServiceManager.Logging;
using ComplianceServiceManager.Models;
using ComplianceServiceManager.Models.AgentSessions;
using ComplianceServiceManager.Models.PortRegistry;
using ComplianceServiceManager.Models.RuntimeProfiles;
using ComplianceServiceManager.Models.ServiceInstances;
using ComplianceServiceManager.Models.SharedRuntime;
using ComplianceServiceManager.Models.Workspaces;
using ComplianceServiceManager.Models.WorkspaceRuntime;
using ComplianceServiceManager.Services;
using ComplianceServiceManager.Services.AgentSessions;
using ComplianceServiceManager.Services.PortRegistry;
using ComplianceServiceManager.Services.RuntimeProfiles;
using ComplianceServiceManager.Services.ServiceInstances;
using ComplianceServiceManager.Services.SharedRuntime;
using ComplianceServiceManager.Services.WorkspaceRuntime;
using ComplianceServiceManager.Services.Workspaces;

namespace ComplianceServiceManager.Tests.WorkspaceRuntime;

public sealed class WorkspaceRuntimeServiceTests : IDisposable
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
    private readonly FakeSessions sessions;
    private readonly RuntimeProfileOrchestrator orchestrator;
    private readonly WorkspaceRuntimeProcessMap launches;
    private readonly WorkspaceRuntimeService runtime;
    private readonly DevelopmentWorkspaceRegistry workspaces;
    private readonly TrackingLogger logger;

    public WorkspaceRuntimeServiceTests()
    {
        root = Path.Combine(Path.GetTempPath(), "aovias-wr-" + Guid.NewGuid().ToString("N"));
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
        sessions = new FakeSessions();
        logger = new TrackingLogger();
        launches = new WorkspaceRuntimeProcessMap();
        workspaces = new DevelopmentWorkspaceRegistry(workspaceStore);
        orchestrator = new RuntimeProfileOrchestrator(
            new RuntimeProfileRegistry(Path.Combine(root, "runtime-profiles.json")),
            new RuntimeProfileValidator(),
            host,
            health,
            logger);
        orchestrator.ConfigureHealthWaitForTests(TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(20));

        runtime = new WorkspaceRuntimeService(
            orchestrator,
            host,
            instances,
            ports,
            availability,
            workspaces,
            health,
            launches,
            logger,
            sessions);
        runtime.ConfigureHealthWaitForTests(TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(20));
        host.Availability = availability;
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
    public void Cursor_Workspace_Discovered()
    {
        var cursor = workspaces.GetEnabledWorkspaces().Single(item => item.Id == "cursor");
        Assert.Equal(WorkspaceAgentOwner.Cursor, cursor.AgentOwner);
        Assert.Equal(cursorPath, cursor.Path, StringComparer.OrdinalIgnoreCase);
        Assert.False(cursor.IsMain);
    }

    [Fact]
    public void Claude_Workspace_Discovered()
    {
        var claude = workspaces.GetEnabledWorkspaces().Single(item => item.Id == "claude");
        Assert.Equal(WorkspaceAgentOwner.Claude, claude.AgentOwner);
        Assert.Equal(claudePath, claude.Path, StringComparer.OrdinalIgnoreCase);
        Assert.False(claude.IsMain);
    }

    [Fact]
    public void BranchMismatch_Blocks()
    {
        SeedIdentityAndOrg();
        PrepareWorkspace("OrganizationService", "cursor", 5101);
        var plan = runtime.Plan(OrgProfile(), "cursor", CursorSnapshot("main"), ["OrganizationService"]);
        Assert.False(plan.CanStart);
        Assert.Contains(plan.Issues, issue => issue.Code == "branch-mismatch");
        Assert.Empty(host.StartCalls);
    }

    [Fact]
    public void MissingWorkspace_Blocks()
    {
        SeedIdentityAndOrg();
        var plan = runtime.Plan(OrgProfile(), "missing", CursorSnapshot(), ["OrganizationService"]);
        Assert.False(plan.CanStart);
        Assert.Contains(plan.Issues, issue => issue.Code == "missing-workspace");
        Assert.Empty(host.StartCalls);
    }

    [Fact]
    public void MissingWorkspacePath_Blocks()
    {
        Directory.Delete(cursorPath, recursive: true);
        SeedIdentityAndOrg();
        var plan = runtime.Plan(OrgProfile(), "cursor", CursorSnapshot(), ["OrganizationService"]);
        Assert.False(plan.CanStart);
        Assert.Contains(plan.Issues, issue => issue.Code == "missing-workspace-path");
    }

    [Fact]
    public void OwnershipMismatch_Blocks()
    {
        var store = Path.Combine(root, "owned-mismatch.json");
        var document = DevelopmentWorkspaceRegistry.BuildDefaults(root);
        document.Repositories[0].Workspaces.Single(item => item.Id == "cursor").AgentOwner = WorkspaceAgentOwner.Unknown;
        File.WriteAllText(store, JsonSerializer.Serialize(document, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        }));
        var owned = CreateRuntime(new DevelopmentWorkspaceRegistry(store));
        SeedIdentityAndOrg();
        PrepareWorkspace("OrganizationService", "cursor", 5101);
        var plan = owned.Plan(OrgProfile(), "cursor", CursorSnapshot(), ["OrganizationService"]);
        Assert.False(plan.CanStart);
        Assert.Contains(plan.Issues, issue => issue.Code == "ownership-mismatch");
    }

    [Fact]
    public void WorkspaceServiceInstance_Created_DistinctFromShared()
    {
        SeedIdentityAndOrg();
        var shared = instances.GetInstancesForService("OrganizationService")
            .Single(item => item.ExecutionScope == ServiceExecutionScope.Shared);
        var workspace = PrepareWorkspace("OrganizationService", "cursor", 5101);
        Assert.Equal(ServiceExecutionScope.Workspace, workspace.ExecutionScope);
        Assert.Equal("cursor", workspace.WorkspaceId);
        Assert.NotEqual(shared.InstanceId, workspace.InstanceId);
        runtime.Plan(OrgProfile(), "cursor", CursorSnapshot(), ["OrganizationService"]);
        Assert.Equal(ServiceExecutionScope.Workspace, workspace.ExecutionScope);
        Assert.Equal(ServiceExecutionScope.Shared, shared.ExecutionScope);
    }

    [Fact]
    public void CursorInstance_HasWorkspaceScope()
    {
        SeedIdentityAndOrg();
        var item = PrepareWorkspace("OrganizationService", "cursor", 5101);
        Assert.Equal(ServiceExecutionScope.Workspace, item.ExecutionScope);
        Assert.Equal("cursor", item.WorkspaceId);
    }

    [Fact]
    public void ClaudeInstance_HasWorkspaceScope()
    {
        SeedIdentityAndOrg();
        var item = PrepareWorkspace("OrganizationService", "claude", 5102);
        Assert.Equal(ServiceExecutionScope.Workspace, item.ExecutionScope);
        Assert.Equal("claude", item.WorkspaceId);
    }

    [Fact]
    public void SharedInstance_CannotBeMutatedByWorkspaceRuntime()
    {
        SeedIdentityAndOrg();
        var shared = instances.GetInstancesForService("OrganizationService")
            .Single(item => item.ExecutionScope == ServiceExecutionScope.Shared);
        var issue = runtime.ValidateInstanceForWorkspaceRuntime(shared, "cursor");
        Assert.NotNull(issue);
        Assert.Equal("shared-instance", issue!.Code);
    }

    [Fact]
    public void Cursor_CannotMutateClaudeInstance()
    {
        var claude = new ServiceInstanceItem
        {
            InstanceId = "claude-org",
            ServiceName = "OrganizationService",
            ExecutionScope = ServiceExecutionScope.Workspace,
            WorkspaceId = "claude"
        };
        var issue = runtime.ValidateInstanceForWorkspaceRuntime(claude, "cursor");
        Assert.Equal("cross-workspace", issue!.Code);
    }

    [Fact]
    public void Claude_CannotMutateCursorInstance()
    {
        var cursor = new ServiceInstanceItem
        {
            InstanceId = "cursor-org",
            ServiceName = "OrganizationService",
            ExecutionScope = ServiceExecutionScope.Workspace,
            WorkspaceId = "cursor"
        };
        var issue = runtime.ValidateInstanceForWorkspaceRuntime(cursor, "claude");
        Assert.Equal("cross-workspace", issue!.Code);
    }

    [Fact]
    public void WorkspacePort_AllocatedFromRegistry()
    {
        SeedIdentityAndOrg();
        PrepareWorkspace("OrganizationService", "cursor", 5101);
        var plan = runtime.Plan(OrgProfile(), "cursor", CursorSnapshot(), ["OrganizationService"]);
        var org = plan.Entries.Single(entry => entry.ServiceName == "OrganizationService");
        Assert.Equal(5101, org.Port);
        Assert.Equal(WorkspaceRuntimeAction.StartWorkspace, org.Action);
        Assert.Equal(0, ports.AssignCalls);
    }

    [Fact]
    public void DuplicateWorkspacePort_Blocked()
    {
        SeedIdentityAndOrg();
        var cursor = PrepareWorkspace("OrganizationService", "cursor", 5101);
        var other = instances.EnsureWorkspaceInstance("Gateway", "Gateway", "cursor");
        ports.ByPort[5101] = new PortAllocationItem { Port = 5101, InstanceId = other.InstanceId, ServiceId = "Gateway" };
        _ = cursor;
        var plan = runtime.Plan(OrgProfile(), "cursor", CursorSnapshot(), ["OrganizationService"]);
        Assert.False(plan.CanStart);
        Assert.Contains(plan.Issues, issue => issue.Code == "port-registry-conflict");
    }

    [Fact]
    public void SharedAndWorkspace_SameService_DifferentPorts_Allowed()
    {
        SeedIdentityAndOrg();
        SetSharedRunning("IdentityService", 7216);
        PrepareWorkspace("OrganizationService", "cursor", 5101);
        var plan = runtime.Plan(OrgProfile(), "cursor", CursorSnapshot(), ["OrganizationService"]);
        Assert.True(plan.CanStart);
        var sharedOrg = host.Services.Single(item => item.Name == "OrganizationService");
        Assert.Equal(7217, sharedOrg.HttpsPort);
        Assert.Equal(5101, plan.Entries.Single(entry => entry.ServiceName == "OrganizationService").Port);
    }

    [Fact]
    public void PortConflict_Blocks()
    {
        SeedIdentityAndOrg();
        PrepareWorkspace("OrganizationService", "cursor", 5101);
        availability.Set(5101, true, pid: 999, name: "foreign.exe");
        var plan = runtime.Plan(OrgProfile(), "cursor", CursorSnapshot(), ["OrganizationService"]);
        Assert.False(plan.CanStart);
        Assert.Contains(plan.Issues, issue => issue.Code == "port-conflict");
        Assert.Contains(plan.Issues, issue => issue.Message.Contains("999"));
    }

    [Fact]
    public void HealthySharedDependency_Reused()
    {
        SeedIdentityAndOrg();
        SetSharedRunning("IdentityService", 7216);
        PrepareWorkspace("OrganizationService", "cursor", 5101);
        var plan = runtime.Plan(OrgProfile(), "cursor", CursorSnapshot(), ["OrganizationService"]);
        Assert.True(plan.CanStart);
        var identity = plan.Entries.Single(entry => entry.ServiceName == "IdentityService");
        var org = plan.Entries.Single(entry => entry.ServiceName == "OrganizationService");
        Assert.Equal(WorkspaceRuntimeAction.ReuseShared, identity.Action);
        Assert.Equal(WorkspaceRuntimeAction.StartWorkspace, org.Action);
        Assert.False(identity.OwnedAfterStart);
        Assert.Contains("7216", identity.Port?.ToString());
        Assert.Empty(host.StartCalls);
    }

    [Fact]
    public async Task RequestedWorkspaceDependency_IsStarted()
    {
        SeedIdentityAndOrg();
        PrepareWorkspace("IdentityService", "cursor", 5100);
        PrepareWorkspace("OrganizationService", "cursor", 5101);
        var result = await runtime.StartAsync(
            OrgProfile(),
            "cursor",
            CursorSnapshot(),
            ["IdentityService", "OrganizationService"]);
        Assert.True(result.Succeeded);
        Assert.Contains("IdentityService", host.StartCalls);
        Assert.Contains("OrganizationService", host.StartCalls);
    }

    [Fact]
    public void DependencyClosure_Validated()
    {
        SeedIdentityAndOrg();
        PrepareWorkspace("OrganizationService", "cursor", 5101);
        var plan = runtime.Plan(OrgProfile(), "cursor", CursorSnapshot(), ["OrganizationService"]);
        Assert.Contains("IdentityService", plan.StartupOrder);
        Assert.Contains("OrganizationService", plan.StartupOrder);
        Assert.True(IndexOf(plan, "IdentityService") < IndexOf(plan, "OrganizationService"));
    }

    [Fact]
    public void DependencyCycle_Blocks()
    {
        AddService("A", 7001);
        AddService("B", 7002);
        var profile = Profile(("A", ["B"]), ("B", ["A"]));
        var plan = runtime.Plan(profile, "cursor", CursorSnapshot(), ["A"]);
        Assert.False(plan.CanStart);
        Assert.Contains(plan.Issues, issue => issue.Message.Contains("Circular", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void WorkspaceProcessSourcePath_IsCursor()
    {
        SeedIdentityAndOrg();
        PrepareWorkspace("OrganizationService", "cursor", 5101);
        var plan = runtime.Plan(OrgProfile(), "cursor", CursorSnapshot(), ["OrganizationService"]);
        var org = plan.Entries.Single(entry => entry.ServiceName == "OrganizationService");
        Assert.True(WorkspaceSourceMapper.IsUnder(org.SourcePath!, cursorPath));
        Assert.False(WorkspaceSourceMapper.IsUnder(org.SourcePath!, mainPath));
        Assert.False(WorkspaceSourceMapper.IsUnder(org.SourcePath!, claudePath));
        var instance = instances.GetInstancesForService("OrganizationService").Single(item => item.WorkspaceId == "cursor");
        Assert.True(launches.TryGetLaunch(instance.InstanceId, out var snapshot));
        Assert.True(WorkspaceSourceMapper.IsUnder(snapshot.Launch.WorkingDirectory, cursorPath));
        Assert.False(WorkspaceSourceMapper.IsUnder(snapshot.Launch.WorkingDirectory, mainPath));
    }

    [Fact]
    public void ClaudeProcessSourcePath_IsClaude()
    {
        SeedIdentityAndOrg();
        SetSharedRunning("IdentityService", 7216);
        PrepareWorkspace("OrganizationService", "claude", 5102);
        var plan = runtime.Plan(OrgProfile(), "claude", ClaudeSnapshot(), ["OrganizationService"]);
        var org = plan.Entries.Single(entry => entry.ServiceName == "OrganizationService");
        Assert.True(plan.CanStart);
        Assert.True(WorkspaceSourceMapper.IsUnder(org.SourcePath!, claudePath));
        Assert.False(WorkspaceSourceMapper.IsUnder(org.SourcePath!, mainPath));
        Assert.False(WorkspaceSourceMapper.IsUnder(org.SourcePath!, cursorPath));
        var instance = instances.GetInstancesForService("OrganizationService").Single(item => item.WorkspaceId == "claude");
        Assert.True(launches.TryGetLaunch(instance.InstanceId, out var snapshot));
        Assert.True(WorkspaceSourceMapper.IsUnder(snapshot.Launch.WorkingDirectory, claudePath));
        Assert.False(WorkspaceSourceMapper.IsUnder(snapshot.Launch.WorkingDirectory, mainPath));
    }

    [Fact]
    public void WorkspacePort_IsInjectedIntoProcessEnvironment()
    {
        SeedIdentityAndOrg();
        SetSharedRunning("IdentityService", 7216);
        host.Services.Single(item => item.Name == "OrganizationService").LaunchProfileName = "https";
        PrepareWorkspace("OrganizationService", "cursor", 5101);
        var plan = runtime.Plan(OrgProfile(), "cursor", CursorSnapshot(), ["OrganizationService"]);
        Assert.True(plan.CanStart);
        var instance = instances.GetInstancesForService("OrganizationService").Single(item => item.WorkspaceId == "cursor");
        Assert.True(launches.TryGetLaunch(instance.InstanceId, out var snapshot));
        Assert.Equal(5101, snapshot.Launch.HttpsPort);
        Assert.Equal("https://localhost:5101", snapshot.Launch.HttpsUrl);
        Assert.Null(snapshot.Launch.LaunchProfileName);
        Assert.True(snapshot.Launch.LaunchEnvironmentVariables.TryGetValue("ASPNETCORE_URLS", out var urls));
        Assert.Contains("5101", urls);
        Assert.DoesNotContain("7217", urls);
        Assert.DoesNotContain("7181", urls);
        Assert.True(snapshot.Launch.SuppressLaunchSettings);
        var (_, arguments) = ServiceLaunchCommand.BuildStartCommand(snapshot.Launch);
        var environment = ServiceLaunchCommand.BuildEnvironmentVariables(snapshot.Launch);
        Assert.Contains("--no-launch-profile", arguments, StringComparison.Ordinal);
        Assert.DoesNotContain("--launch-profile", arguments, StringComparison.Ordinal);
        Assert.Equal("https://localhost:5101", environment["ASPNETCORE_URLS"]);
        Assert.DoesNotContain("5085", environment["ASPNETCORE_URLS"]);
        Assert.Equal(7217, host.Services.Single(item => item.Name == "OrganizationService").HttpsPort);
    }

    [Fact]
    public async Task WorkspaceStart_BindsRegisteredPort_NotLaunchSettings()
    {
        SeedIdentityAndOrg();
        SetSharedRunning("IdentityService", 7216);
        host.Services.Single(item => item.Name == "OrganizationService").LaunchProfileName = "http";
        PrepareWorkspace("OrganizationService", "cursor", 5101);
        var result = await runtime.StartAsync(OrgProfile(), "cursor", CursorSnapshot(), ["OrganizationService"]);
        Assert.True(result.Succeeded);
        Assert.NotNull(host.LastStartCommand);
        Assert.Contains("--no-launch-profile", host.LastStartCommand!.Value.Arguments, StringComparison.Ordinal);
        Assert.DoesNotContain("--launch-profile", host.LastStartCommand.Value.Arguments, StringComparison.Ordinal);
        Assert.Equal("https://localhost:5101", host.LastStartEnvironment!["ASPNETCORE_URLS"]);
        Assert.DoesNotContain("5085", host.LastStartEnvironment["ASPNETCORE_URLS"]);
        Assert.True(availability.Observe(5101).IsListening);
        Assert.False(availability.Observe(5085).IsListening);
        var instance = instances.GetInstancesForService("OrganizationService").Single(item => item.WorkspaceId == "cursor");
        Assert.True(launches.TryGetLaunch(instance.InstanceId, out var snapshot));
        Assert.Equal(5101, snapshot.Port);
        Assert.Equal(snapshot.Port, snapshot.Launch.HttpsPort);
        Assert.Equal(availability.Observe(5101).ProcessId, snapshot.ProcessId);
        Assert.Contains(5101, health.ProbedPorts);
    }

    [Fact]
    public void SharedProcessSourcePath_RemainsCanonical()
    {
        SeedIdentityAndOrg();
        SetSharedRunning("IdentityService", 7216);
        PrepareWorkspace("OrganizationService", "cursor", 5101);
        var plan = runtime.Plan(OrgProfile(), "cursor", CursorSnapshot(), ["OrganizationService"]);
        var identity = plan.Entries.Single(entry => entry.ServiceName == "IdentityService");
        Assert.Contains(mainPath, identity.SourcePath, StringComparison.OrdinalIgnoreCase);
        Assert.True(WorkspaceSourceMapper.IsUnder(
            host.Services.Single(item => item.Name == "IdentityService").ProjectPath,
            mainPath));
    }

    [Fact]
    public async Task WorkspaceHealth_ProbesWorkspacePort()
    {
        SeedIdentityAndOrg();
        SetSharedRunning("IdentityService", 7216);
        PrepareWorkspace("OrganizationService", "cursor", 5101);
        var result = await runtime.StartAsync(OrgProfile(), "cursor", CursorSnapshot(), ["OrganizationService"]);
        Assert.True(result.Succeeded);
        Assert.Contains(5101, health.ProbedPorts);
        Assert.DoesNotContain(7217, health.ProbedPorts);
    }

    [Fact]
    public async Task Ownership_AssignedOnlyToStartedServices()
    {
        SeedIdentityAndOrg();
        SetSharedRunning("IdentityService", 7216);
        PrepareWorkspace("OrganizationService", "cursor", 5101);
        await runtime.StartAsync(OrgProfile(), "cursor", CursorSnapshot(), ["OrganizationService"]);
        var owned = runtime.GetSession("cursor").OwnedServiceNames;
        Assert.Contains("OrganizationService", owned);
        Assert.DoesNotContain("IdentityService", owned);
    }

    [Fact]
    public async Task ReusedShared_NotClaimed()
    {
        SeedIdentityAndOrg();
        SetSharedRunning("IdentityService", 7216);
        PrepareWorkspace("OrganizationService", "cursor", 5101);
        await runtime.StartAsync(OrgProfile(), "cursor", CursorSnapshot(), ["OrganizationService"]);
        Assert.DoesNotContain("IdentityService", launches.GetOwnedServiceNames("cursor"), StringComparer.OrdinalIgnoreCase);
        Assert.Equal(ServiceStatus.Running, host.Services.Single(item => item.Name == "IdentityService").Status);
    }

    [Fact]
    public async Task Stop_StopsOnlyWorkspaceOwned()
    {
        SeedIdentityAndOrg();
        SetSharedRunning("IdentityService", 7216);
        PrepareWorkspace("OrganizationService", "cursor", 5101);
        await runtime.StartAsync(OrgProfile(), "cursor", CursorSnapshot(), ["OrganizationService"]);
        host.StopCalls.Clear();
        await runtime.StopAsync("cursor");
        Assert.Equal(["OrganizationService"], host.StopCalls);
        Assert.Equal(ServiceStatus.Running, host.Services.Single(item => item.Name == "IdentityService").Status);
        Assert.Empty(runtime.GetSession("cursor").OwnedServiceNames);
    }

    [Fact]
    public async Task StopCursor_DoesNotStopClaude()
    {
        SeedIdentityAndOrg();
        SetSharedRunning("IdentityService", 7216);
        PrepareWorkspace("OrganizationService", "cursor", 5101);
        PrepareWorkspace("OrganizationService", "claude", 5102);
        await runtime.StartAsync(OrgProfile(), "cursor", CursorSnapshot(), ["OrganizationService"]);
        await runtime.StartAsync(OrgProfile(), "claude", ClaudeSnapshot(), ["OrganizationService"]);
        host.StopCalls.Clear();
        await runtime.StopAsync("cursor");
        Assert.Equal(["OrganizationService"], host.StopCalls);
        Assert.Equal(SharedRuntimeStatus.Running, runtime.GetSession("claude").Status);
        Assert.Contains("OrganizationService", runtime.GetSession("claude").OwnedServiceNames);
        Assert.Empty(runtime.GetSession("cursor").OwnedServiceNames);
    }

    [Fact]
    public async Task SuccessfulStart_EstablishesOwnership()
    {
        SeedIdentityAndOrg();
        SetSharedRunning("IdentityService", 7216);
        PrepareWorkspace("OrganizationService", "cursor", 5101);
        var result = await runtime.StartAsync(OrgProfile(), "cursor", CursorSnapshot(), ["OrganizationService"]);
        Assert.True(result.Succeeded);
        Assert.Equal(SharedRuntimeStatus.Running, result.Status);
        Assert.Equal(SharedRuntimeStatus.Running, runtime.GetSession("cursor").Status);
        Assert.Contains("OrganizationService", runtime.GetSession("cursor").OwnedServiceNames);
        var instance = instances.GetInstancesForService("OrganizationService").Single(item => item.WorkspaceId == "cursor");
        Assert.True(launches.TryGetLaunch(instance.InstanceId, out var snapshot));
        Assert.True(snapshot.Owned);
        Assert.True(snapshot.StartedByRuntime);
        Assert.NotNull(snapshot.ProcessId);
        Assert.Equal(ServiceStatus.Running, snapshot.Launch.Status);
        Assert.Equal(HealthStatus.Healthy, snapshot.Launch.Health);
    }

    [Fact]
    public async Task HealthFailure_TerminatesExactProcess_NoOrphan_NoOwnership()
    {
        SeedIdentityAndOrg();
        SetSharedRunning("IdentityService", 7216);
        PrepareWorkspace("OrganizationService", "cursor", 5101);
        health.ForcedStatus = HealthStatus.Unreachable;
        var result = await runtime.StartAsync(OrgProfile(), "cursor", CursorSnapshot(), ["OrganizationService"]);
        Assert.False(result.Succeeded);
        Assert.Equal(SharedRuntimeStatus.Failed, result.Status);
        Assert.Equal(SharedRuntimeStatus.Failed, runtime.GetSession("cursor").Status);
        Assert.Empty(runtime.GetSession("cursor").OwnedServiceNames);
        Assert.Equal(["OrganizationService"], host.StopCalls);
        Assert.NotEmpty(host.StoppedProcessIds);
        Assert.False(availability.Observe(5101).IsListening);
        Assert.Equal(ServiceStatus.Running, host.Services.Single(item => item.Name == "IdentityService").Status);
        Assert.Equal(4242, host.Services.Single(item => item.Name == "IdentityService").ProcessId);
        Assert.Contains("terminated", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Ownership was not claimed", result.Error, StringComparison.OrdinalIgnoreCase);
        var instance = instances.GetInstancesForService("OrganizationService").Single(item => item.WorkspaceId == "cursor");
        Assert.False(launches.TryGetLaunch(instance.InstanceId, out var leftover) && leftover.Owned);
    }

    [Fact]
    public async Task HealthTimeout_TerminatesExactProcess_NoOrphan()
    {
        SeedIdentityAndOrg();
        SetSharedRunning("IdentityService", 7216);
        PrepareWorkspace("OrganizationService", "cursor", 5101);
        health.ForcedStatus = HealthStatus.Unreachable;
        health.Stall = TimeSpan.FromMilliseconds(80);
        var result = await runtime.StartAsync(OrgProfile(), "cursor", CursorSnapshot(), ["OrganizationService"]);
        Assert.False(result.Succeeded);
        Assert.Equal(SharedRuntimeStatus.Failed, runtime.GetSession("cursor").Status);
        Assert.Empty(runtime.GetSession("cursor").OwnedServiceNames);
        Assert.Equal(["OrganizationService"], host.StopCalls);
        Assert.False(availability.Observe(5101).IsListening);
        Assert.DoesNotContain("IdentityService", host.StopCalls);
    }

    [Fact]
    public async Task Cancellation_TerminatesExactProcess_NotRunning()
    {
        SeedIdentityAndOrg();
        SetSharedRunning("IdentityService", 7216);
        PrepareWorkspace("OrganizationService", "cursor", 5101);
        health.Stall = TimeSpan.FromSeconds(5);
        using var cts = new CancellationTokenSource();
        var start = runtime.StartAsync(OrgProfile(), "cursor", CursorSnapshot(), ["OrganizationService"], cancellationToken: cts.Token);
        await Task.Delay(40);
        cts.Cancel();
        var result = await start;
        Assert.False(result.Succeeded);
        Assert.Contains("cancelled", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.NotEqual(SharedRuntimeStatus.Running, result.Status);
        Assert.Equal(SharedRuntimeStatus.Failed, runtime.GetSession("cursor").Status);
        Assert.Empty(runtime.GetSession("cursor").OwnedServiceNames);
        Assert.Equal(["OrganizationService"], host.StopCalls);
        Assert.False(availability.Observe(5101).IsListening);
        Assert.Equal(ServiceStatus.Running, host.Services.Single(item => item.Name == "IdentityService").Status);
    }

    [Fact]
    public async Task FailedStart_DoesNotTerminateSharedOrClaude()
    {
        SeedIdentityAndOrg();
        SetSharedRunning("IdentityService", 7216);
        SetSharedRunning("OrganizationService", 7217);
        PrepareWorkspace("OrganizationService", "cursor", 5101);
        PrepareWorkspace("OrganizationService", "claude", 5102);
        await runtime.StartAsync(OrgProfile(), "claude", ClaudeSnapshot(), ["OrganizationService"]);
        var claudeInstance = instances.GetInstancesForService("OrganizationService")
            .Single(item => item.WorkspaceId == "claude");
        Assert.True(launches.TryGetLaunch(claudeInstance.InstanceId, out var claudeLaunch));
        var claudeProcessId = claudeLaunch.ProcessId;
        host.StopCalls.Clear();
        health.ForcedStatus = HealthStatus.Unreachable;
        var result = await runtime.StartAsync(OrgProfile(), "cursor", CursorSnapshot(), ["OrganizationService"]);
        Assert.False(result.Succeeded);
        Assert.Equal(["OrganizationService"], host.StopCalls);
        Assert.Equal(SharedRuntimeStatus.Running, runtime.GetSession("claude").Status);
        Assert.Contains("OrganizationService", runtime.GetSession("claude").OwnedServiceNames);
        Assert.True(launches.TryGetLaunch(claudeInstance.InstanceId, out var claudeAfter));
        Assert.Equal(claudeProcessId, claudeAfter.ProcessId);
        Assert.Equal(ServiceStatus.Running, host.Services.Single(item => item.Name == "IdentityService").Status);
        Assert.Equal(4242, host.Services.Single(item => item.Name == "IdentityService").ProcessId);
        Assert.Equal(ServiceStatus.Running, host.Services.Single(item => item.Name == "OrganizationService").Status);
    }

    [Fact]
    public async Task Restart_DoesNotDuplicateProcess()
    {
        SeedIdentityAndOrg();
        SetSharedRunning("IdentityService", 7216);
        PrepareWorkspace("OrganizationService", "cursor", 5101);
        var first = await runtime.StartAsync(OrgProfile(), "cursor", CursorSnapshot(), ["OrganizationService"]);
        Assert.True(first.Succeeded);
        var instance = instances.GetInstancesForService("OrganizationService").Single(item => item.WorkspaceId == "cursor");
        Assert.True(launches.TryGetLaunch(instance.InstanceId, out var firstLaunch));
        var firstPid = firstLaunch.ProcessId;
        host.StartCalls.Clear();
        var stopped = await runtime.StopAsync("cursor");
        Assert.True(stopped.Succeeded);
        Assert.Empty(runtime.GetSession("cursor").OwnedServiceNames);
        Assert.False(availability.Observe(5101).IsListening);
        var second = await runtime.StartAsync(OrgProfile(), "cursor", CursorSnapshot(), ["OrganizationService"]);
        Assert.True(second.Succeeded);
        Assert.Equal(["OrganizationService"], host.StartCalls);
        Assert.True(launches.TryGetLaunch(instance.InstanceId, out var secondLaunch));
        Assert.NotEqual(firstPid, secondLaunch.ProcessId);
        Assert.Single(runtime.GetSession("cursor").OwnedServiceNames);
        Assert.True(availability.Observe(5101).IsListening);
        Assert.Equal(secondLaunch.ProcessId, availability.Observe(5101).ProcessId);
    }

    [Fact]
    public async Task WrongPort_IsRejected_AndProcessTerminated()
    {
        SeedIdentityAndOrg();
        SetSharedRunning("IdentityService", 7216);
        PrepareWorkspace("OrganizationService", "cursor", 5101);
        host.BindPortOverride = 5085;
        var result = await runtime.StartAsync(OrgProfile(), "cursor", CursorSnapshot(), ["OrganizationService"]);
        Assert.False(result.Succeeded);
        Assert.Contains("5101", result.Error);
        Assert.Contains("different port", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(["OrganizationService"], host.StopCalls);
        Assert.Empty(runtime.GetSession("cursor").OwnedServiceNames);
        Assert.False(availability.Observe(5101).IsListening);
        Assert.False(availability.Observe(5085).IsListening);
        Assert.Equal(SharedRuntimeStatus.Failed, runtime.GetSession("cursor").Status);
    }

    [Fact]
    public async Task CleanupFailure_SurfacesNeedsAttention_WithPid()
    {
        SeedIdentityAndOrg();
        SetSharedRunning("IdentityService", 7216);
        PrepareWorkspace("OrganizationService", "cursor", 5101);
        health.ForcedStatus = HealthStatus.Unreachable;
        host.StopLeavesProcessAlive = true;
        var result = await runtime.StartAsync(OrgProfile(), "cursor", CursorSnapshot(), ["OrganizationService"]);
        Assert.False(result.Succeeded);
        Assert.Contains("Needs attention", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PID", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("terminated", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(SharedRuntimeStatus.Failed, runtime.GetSession("cursor").Status);
        Assert.NotEqual(SharedRuntimeStatus.Stopped, runtime.GetSession("cursor").Status);
        Assert.Empty(runtime.GetSession("cursor").OwnedServiceNames);
        var instance = instances.GetInstancesForService("OrganizationService").Single(item => item.WorkspaceId == "cursor");
        Assert.True(launches.TryGetLaunch(instance.InstanceId, out var leftover));
        Assert.False(leftover.Owned);
        Assert.True(leftover.StartedByRuntime);
        Assert.NotNull(leftover.ProcessId);
        Assert.Equal(ServiceStatus.Error, leftover.Launch.Status);
        Assert.Contains("Needs attention", leftover.Launch.LastAction, StringComparison.OrdinalIgnoreCase);
        Assert.True(availability.Observe(5101).IsListening);
        Assert.Equal(leftover.ProcessId, availability.Observe(5101).ProcessId);
        Assert.Equal(["OrganizationService"], host.StopCalls);
        Assert.Equal(ServiceStatus.Running, host.Services.Single(item => item.Name == "IdentityService").Status);
    }

    [Fact]
    public async Task DuplicateStart_ReusesHealthyWorkspaceInstance()
    {
        SeedIdentityAndOrg();
        SetSharedRunning("IdentityService", 7216);
        PrepareWorkspace("OrganizationService", "cursor", 5101);
        await runtime.StartAsync(OrgProfile(), "cursor", CursorSnapshot(), ["OrganizationService"]);
        host.StartCalls.Clear();
        var second = await runtime.StartAsync(OrgProfile(), "cursor", CursorSnapshot(), ["OrganizationService"]);
        Assert.True(second.Succeeded);
        Assert.Empty(host.StartCalls);
        Assert.Equal(WorkspaceRuntimeAction.ReuseWorkspace, second.Plan!.Entries.Single(entry => entry.ServiceName == "OrganizationService").Action);
    }

    [Fact]
    public async Task UnhealthyExternal_NotBlindlyRestarted()
    {
        SeedIdentityAndOrg();
        SetSharedRunning("IdentityService", 7216);
        PrepareWorkspace("OrganizationService", "cursor", 5101);
        await runtime.StartAsync(OrgProfile(), "cursor", CursorSnapshot(), ["OrganizationService"]);
        var instance = instances.GetInstancesForService("OrganizationService")
            .Single(item => item.WorkspaceId == "cursor");
        Assert.True(launches.TryGetLaunch(instance.InstanceId, out var snapshot));
        snapshot.Launch.Health = HealthStatus.Unreachable;
        snapshot.Launch.Status = ServiceStatus.Running;
        host.StartCalls.Clear();
        var result = await runtime.StartAsync(OrgProfile(), "cursor", CursorSnapshot(), ["OrganizationService"]);
        Assert.True(result.Blocked);
        Assert.Empty(host.StartCalls);
        Assert.Contains(result.Plan!.Issues, issue => issue.Code == "unhealthy-running");
    }

    [Fact]
    public void ResourceIntelligence_IsNotARuntimeDependency()
    {
        var ctor = typeof(WorkspaceRuntimeService).GetConstructors().Single();
        var types = ctor.GetParameters().Select(parameter => parameter.ParameterType).ToArray();
        Assert.DoesNotContain(types, type => type.Name.Contains("LocalResource", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(types, type => type.Name.Contains("ResourceIntelligence", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(types, type => type.Name.Contains("Git", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Plan_DoesNotSwitchProfilesOrReassignPorts()
    {
        SeedIdentityAndOrg();
        PrepareWorkspace("OrganizationService", "cursor", 5101);
        var before = orchestrator.GetProfiles().Select(profile => profile.Id).ToList();
        _ = runtime.Plan(OrgProfile(), "cursor", CursorSnapshot(), ["OrganizationService"]);
        Assert.Equal(before, orchestrator.GetProfiles().Select(profile => profile.Id).ToList());
        Assert.Equal(0, ports.AssignCalls);
        Assert.Equal(0, ports.ReleaseCalls);
    }

    [Fact]
    public void Plan_DoesNotMutateGitWorktreesOrAgents()
    {
        SeedIdentityAndOrg();
        PrepareWorkspace("OrganizationService", "cursor", 5101);
        _ = runtime.Plan(OrgProfile(), "cursor", CursorSnapshot(), ["OrganizationService"]);
        Assert.Empty(host.StartCalls);
        Assert.Empty(host.StopCalls);
        Assert.DoesNotContain(logger.Messages, message => message.Contains("git ", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(logger.Messages, message => message.Contains("worktree", StringComparison.OrdinalIgnoreCase));
        Assert.Empty(sessions.Mutations);
    }

    [Fact]
    public void AgentSessionMismatch_Blocks()
    {
        SeedIdentityAndOrg();
        PrepareWorkspace("OrganizationService", "cursor", 5101);
        sessions.Sessions.Add(new AgentSessionItem
        {
            Id = "sess-1",
            WorkspaceId = "claude",
            Status = AgentSessionStatus.Active
        });
        var plan = runtime.Plan(OrgProfile(), "cursor", CursorSnapshot(), ["OrganizationService"], "sess-1");
        Assert.False(plan.CanStart);
        Assert.Contains(plan.Issues, issue => issue.Code == "agent-session-mismatch");
    }

    [Fact]
    public void StaleAgentSession_WarnsButDoesNotBlock()
    {
        SeedIdentityAndOrg();
        SetSharedRunning("IdentityService", 7216);
        PrepareWorkspace("OrganizationService", "cursor", 5101);
        sessions.Sessions.Add(new AgentSessionItem
        {
            Id = "sess-stale",
            WorkspaceId = "cursor",
            Status = AgentSessionStatus.Stale
        });
        var plan = runtime.Plan(OrgProfile(), "cursor", CursorSnapshot(), ["OrganizationService"], "sess-stale");
        Assert.True(plan.CanStart);
        Assert.Contains(plan.Warnings, warning => warning.Contains("stale", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void WebApp_CanIsolateWhenSourceExists()
    {
        SeedIdentityAndOrg();
        SetSharedRunning("IdentityService", 7216);
        SetSharedRunning("OrganizationService", 7217);
        AddService("WebApp", 4200, kind: ServiceKind.AngularWebApp);
        PrepareWorkspace("WebApp", "cursor", 4201);
        var profile = Profile(("IdentityService", []), ("OrganizationService", ["IdentityService"]), ("WebApp", ["OrganizationService"]));
        var plan = runtime.Plan(profile, "cursor", CursorSnapshot(), ["WebApp"]);
        Assert.True(plan.CanStart);
        var web = plan.Entries.Single(entry => entry.ServiceName == "WebApp");
        Assert.Equal(WorkspaceRuntimeAction.StartWorkspace, web.Action);
        Assert.Equal(4201, web.Port);
        Assert.Contains(cursorPath, web.SourcePath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WebApp_BlocksWhenWorkspaceSourceMissing()
    {
        SeedIdentityAndOrg();
        SetSharedRunning("IdentityService", 7216);
        SetSharedRunning("OrganizationService", 7217);
        AddService("WebApp", 4200, kind: ServiceKind.AngularWebApp);
        var item = instances.EnsureWorkspaceInstance("WebApp", "WebApp", "cursor");
        ports.ByInstance[item.InstanceId] = new PortAllocationItem
        {
            Port = 4201,
            InstanceId = item.InstanceId,
            ServiceId = "WebApp"
        };
        var profile = Profile(("IdentityService", []), ("OrganizationService", ["IdentityService"]), ("WebApp", ["OrganizationService"]));
        var plan = runtime.Plan(profile, "cursor", CursorSnapshot(), ["WebApp"]);
        Assert.False(plan.CanStart);
        Assert.Contains(plan.Issues, issue => issue.Code == "missing-workspace-source");
    }

    [Fact]
    public async Task SharedRuntime_StillPlansAfterWorkspaceStart()
    {
        SeedIdentityAndOrg();
        SetSharedRunning("IdentityService", 7216);
        PrepareWorkspace("OrganizationService", "cursor", 5101);
        await runtime.StartAsync(OrgProfile(), "cursor", CursorSnapshot(), ["OrganizationService"]);
        var shared = new SharedRuntimeService(orchestrator, host, instances, ports, availability, workspaces, logger);
        var plan = shared.Plan(OrgProfile());
        Assert.NotNull(plan);
        Assert.Equal("Shared", plan.ScopeDisplay);
        Assert.DoesNotContain(plan.Entries, entry => entry.Action == SharedRuntimeAction.Start && entry.ScopeDisplay != "Shared");
    }

    [Fact]
    public void UnhealthySharedDependency_BlocksInsteadOfDuplicating()
    {
        SeedIdentityAndOrg();
        PrepareWorkspace("OrganizationService", "cursor", 5101);
        var plan = runtime.Plan(OrgProfile(), "cursor", CursorSnapshot(), ["OrganizationService"]);
        Assert.False(plan.CanStart);
        Assert.Contains(plan.Issues, issue => issue.Code == "unhealthy-shared-dependency");
        Assert.Empty(host.StartCalls);
    }

    [Fact]
    public void MainWorkspace_CannotBeUsedAsWorkspaceRuntime()
    {
        SeedIdentityAndOrg();
        var plan = runtime.Plan(OrgProfile(), "main", new DevelopmentWorkspaceItem
        {
            Id = "main",
            Branch = "main",
            Availability = WorkspaceAvailability.Clean
        }, ["OrganizationService"]);
        Assert.False(plan.CanStart);
        Assert.Contains(plan.Issues, issue => issue.Code == "missing-workspace");
    }

    private WorkspaceRuntimeService CreateRuntime(DevelopmentWorkspaceRegistry registry)
        => new(
            orchestrator,
            host,
            instances,
            ports,
            availability,
            registry,
            health,
            launches,
            logger,
            sessions);

    private void SeedIdentityAndOrg()
    {
        AddService("IdentityService", 7216);
        AddService("OrganizationService", 7217);
    }

    private RuntimeProfileItem OrgProfile()
        => Profile(("IdentityService", []), ("OrganizationService", ["IdentityService"]));

    private void SetSharedRunning(string name, int port)
    {
        var service = host.Services.Single(item => item.Name == name);
        service.Status = ServiceStatus.Running;
        service.Health = HealthStatus.Healthy;
        service.HttpsPort = port;
        service.ProcessId = 4242;
    }

    private ServiceInstanceItem PrepareWorkspace(string service, string workspaceId, int port)
    {
        var target = workspaceId == "claude" ? claudePath : cursorPath;
        Directory.CreateDirectory(Path.Combine(target, service));
        var item = instances.EnsureWorkspaceInstance(service, service, workspaceId);
        ports.ByInstance[item.InstanceId] = new PortAllocationItem
        {
            Port = port,
            InstanceId = item.InstanceId,
            ServiceId = service
        };
        return item;
    }

    private void AddService(string name, int? port = 7000, ServiceKind kind = ServiceKind.WebApi)
    {
        var path = Path.Combine(mainPath, name);
        Directory.CreateDirectory(path);
        var instance = new ServiceInstanceItem
        {
            InstanceId = Guid.NewGuid().ToString("N"),
            ServiceId = name,
            ServiceName = name,
            ExecutionScope = ServiceExecutionScope.Shared,
            Ownership = ServiceInstanceOwnership.Shared,
            IsConfigured = true
        };
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
            Status = ServiceStatus.Stopped,
            Health = HealthStatus.Unknown,
            Kind = kind
        });
    }

    private DevelopmentWorkspaceItem CursorSnapshot(string branch = "agent/cursor/studio-v2-workspace-runtime")
        => new()
        {
            Id = "cursor",
            DisplayName = "CURSOR",
            Path = cursorPath,
            Branch = branch,
            Head = "abc123",
            Availability = WorkspaceAvailability.Clean,
            AgentOwner = WorkspaceAgentOwner.Cursor
        };

    private DevelopmentWorkspaceItem ClaudeSnapshot(string branch = "agent/claude/active")
        => new()
        {
            Id = "claude",
            DisplayName = "CLAUDE",
            Path = claudePath,
            Branch = branch,
            Head = "def456",
            Availability = WorkspaceAvailability.Clean,
            AgentOwner = WorkspaceAgentOwner.Claude
        };

    private static RuntimeProfileItem Profile(params (string Name, string[] DependsOn)[] services)
        => RuntimeProfileItem.FromDefinition(new RuntimeProfileDefinition
        {
            Id = "test-workspace-profile",
            Name = "Test Workspace",
            Enabled = true,
            MaxParallelStarts = 2,
            Services = services.Select(item => new RuntimeProfileServiceEntry
            {
                ServiceName = item.Name,
                Enabled = true,
                DependsOn = item.DependsOn.ToList()
            }).ToList()
        });

    private static int IndexOf(WorkspaceRuntimePlan plan, string name)
        => plan.StartupOrder
            .Select((value, index) => (value, index))
            .First(pair => string.Equals(pair.value, name, StringComparison.OrdinalIgnoreCase))
            .index;

    private sealed class FakeInstances : IServiceInstanceService
    {
        public List<ServiceInstanceItem> Items { get; } = [];

        public event Action? InstancesChanged;

        public IReadOnlyList<ServiceInstanceItem> GetInstances() => Items;

        public IReadOnlyList<ServiceInstanceItem> GetInstancesForService(string serviceId)
            => Items.Where(item => string.Equals(item.ServiceId, serviceId, StringComparison.OrdinalIgnoreCase)).ToList();

        public Task RefreshAsync(CancellationToken cancellationToken = default)
        {
            InstancesChanged?.Invoke();
            return Task.CompletedTask;
        }

        public ServiceInstanceItem EnsureSharedInstance(string serviceId, string serviceName)
            => Items.First(item => item.ServiceId == serviceId && item.ExecutionScope == ServiceExecutionScope.Shared);

        public ServiceInstanceItem EnsureWorkspaceInstance(
            string serviceId,
            string serviceName,
            string workspaceId,
            string? agentSessionId = null,
            string? runtimeProfileId = null)
        {
            var existing = Items.FirstOrDefault(item =>
                item.ExecutionScope == ServiceExecutionScope.Workspace
                && string.Equals(item.ServiceId, serviceId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(item.WorkspaceId, workspaceId, StringComparison.OrdinalIgnoreCase));
            if (existing is not null)
            {
                return existing;
            }

            var created = new ServiceInstanceItem
            {
                InstanceId = Guid.NewGuid().ToString("N"),
                ServiceId = serviceId,
                ServiceName = serviceName,
                ExecutionScope = ServiceExecutionScope.Workspace,
                Ownership = ServiceInstanceOwnership.Workspace,
                WorkspaceId = workspaceId,
                AgentSessionId = agentSessionId,
                RuntimeProfileId = runtimeProfileId,
                IsConfigured = true,
                ScopeDisplay = ServiceInstanceItem.FormatScopeDisplay(ServiceExecutionScope.Workspace, workspaceId)
            };
            Items.Add(created);
            return created;
        }

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
        private readonly Dictionary<string, int> boundPorts = new(StringComparer.OrdinalIgnoreCase);
        private int nextProcessId = 10_000;

        public ObservableCollection<ServiceInfoModel> Services { get; } = [];
        public List<string> StartCalls { get; } = [];
        public List<string> StopCalls { get; } = [];
        public List<int> StoppedProcessIds { get; } = [];
        public FakeAvailability? Availability { get; set; }
        public int? BindPortOverride { get; set; }
        public bool StopLeavesProcessAlive { get; set; }
        public (string FileName, string Arguments)? LastStartCommand { get; private set; }
        public IReadOnlyDictionary<string, string>? LastStartEnvironment { get; private set; }

        public Task StartAsync(ServiceInfoModel service, CancellationToken cancellationToken)
        {
            LastStartCommand = ServiceLaunchCommand.BuildStartCommand(service);
            LastStartEnvironment = ServiceLaunchCommand.BuildEnvironmentVariables(service);
            StartCalls.Add(service.Name);
            service.Status = ServiceStatus.Running;
            service.Health = HealthStatus.Healthy;
            service.ProcessId = ++nextProcessId;
            service.LastAction = "Started";

            var registered = service.HttpsPort ?? service.HttpPort ?? service.Port;
            var bindPort = BindPortOverride ?? registered;
            if (bindPort is int bound)
            {
                boundPorts[service.Name] = bound;
                Availability?.Set(bound, true, service.ProcessId, service.Name);
                if (registered is int expected && expected != bound)
                {
                    Availability?.Set(expected, false);
                }
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(ServiceInfoModel service, CancellationToken cancellationToken)
        {
            StopCalls.Add(service.Name);
            if (service.ProcessId is int pid)
            {
                StoppedProcessIds.Add(pid);
            }

            if (StopLeavesProcessAlive)
            {
                return Task.CompletedTask;
            }

            if (boundPorts.TryGetValue(service.Name, out var bound))
            {
                Availability?.Set(bound, false);
                boundPorts.Remove(service.Name);
            }

            service.Status = ServiceStatus.Stopped;
            service.ProcessId = null;
            service.LastAction = "Stopped";
            return Task.CompletedTask;
        }
    }

    private sealed class FakeHealthProbe : IHealthProbe
    {
        public List<int?> ProbedPorts { get; } = [];
        public HealthStatus? ForcedStatus { get; set; }
        public TimeSpan? Stall { get; set; }

        public async Task<HealthStatus> CheckAsync(ServiceInfoModel service, CancellationToken cancellationToken)
        {
            ProbedPorts.Add(service.HttpsPort ?? service.HttpPort ?? service.Port);
            if (Stall is TimeSpan delay)
            {
                await Task.Delay(delay, cancellationToken);
            }

            if (ForcedStatus is HealthStatus forced)
            {
                return forced;
            }

            return service.Status == ServiceStatus.Running ? HealthStatus.Healthy : HealthStatus.Unknown;
        }
    }

    private sealed class FakeSessions : IAgentSessionService
    {
        public List<AgentSessionItem> Sessions { get; } = [];
        public List<string> Mutations { get; } = [];

        public IReadOnlyList<AgentSessionItem> GetSessions() => Sessions;

        public event Action? SessionsChanged;

        public Task RefreshAllAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task RefreshAsync(AgentSessionItem session, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<AgentSessionItem> CreateAsync(
            DevelopmentAgentType agentType,
            string title,
            string description,
            string? taskId,
            string workspaceId,
            string expectedBranch,
            string? runtimeProfileId,
            string? pullRequestId,
            bool allowWorkspaceOverride = false,
            CancellationToken cancellationToken = default)
        {
            Mutations.Add("create");
            throw new NotSupportedException();
        }

        public Task StartAsync(AgentSessionItem session, CancellationToken cancellationToken = default)
        {
            Mutations.Add("start");
            return Task.CompletedTask;
        }

        public Task PauseAsync(AgentSessionItem session)
        {
            Mutations.Add("pause");
            return Task.CompletedTask;
        }

        public Task ResumeAsync(AgentSessionItem session, CancellationToken cancellationToken = default)
        {
            Mutations.Add("resume");
            return Task.CompletedTask;
        }

        public Task CompleteAsync(AgentSessionItem session)
        {
            Mutations.Add("complete");
            return Task.CompletedTask;
        }

        public Task CancelAsync(AgentSessionItem session)
        {
            Mutations.Add("cancel");
            return Task.CompletedTask;
        }

        public IReadOnlyList<(string Id, string DisplayName, string Path, DevelopmentAgentType? PreferredAgent)> GetWorkspaceChoices()
            => [];

        public IReadOnlyList<(string Id, string Name)> GetRuntimeProfileChoices() => [];

        public IReadOnlyList<(string Id, string Title)> GetPullRequestChoices() => [];
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
