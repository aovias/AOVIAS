using System.Collections.ObjectModel;
using System.IO;
using ComplianceServiceManager.Logging;
using ComplianceServiceManager.Models;
using ComplianceServiceManager.Models.ServiceInstances;
using ComplianceServiceManager.Models.Workspaces;
using ComplianceServiceManager.Services;
using ComplianceServiceManager.Services.ServiceInstances;
using ComplianceServiceManager.Services.Workspaces;

namespace ComplianceServiceManager.Tests.ServiceInstances;

public sealed class ServiceInstanceModelTests : IDisposable
{
    private readonly string root;
    private readonly string storePath;
    private readonly string workspaceStore;
    private readonly FakeServiceRuntimeHost host;
    private readonly ServiceInstanceService service;
    private readonly GitCliRunner gitRunner;

    public ServiceInstanceModelTests()
    {
        root = Path.Combine(Path.GetTempPath(), "aovias-si-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var main = Path.Combine(root, "AOVIAS");
        var cursor = Path.Combine(root, "AOVIAS.Cursor");
        var claude = Path.Combine(root, "AOVIAS.Claude");
        Directory.CreateDirectory(main);

        InitGit(main, "main");
        InitGitWorktree(main, cursor, "agent/cursor/organization-feature");
        InitGitWorktree(main, claude, "agent/claude/def-env-004-server-log-errors");

        storePath = Path.Combine(root, "service-instances.json");
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

        var registry = new DevelopmentWorkspaceRegistry(workspaceStore);
        gitRunner = new GitCliRunner(() => new[] { root, main, cursor, claude });
        service = new ServiceInstanceService(
            new ServiceInstanceRegistry(storePath),
            registry,
            new GitWorkspaceService(gitRunner),
            host,
            new NoopLogger());
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
    public void SharedInstance_Created_WithoutWorkspace()
    {
        var shared = service.EnsureSharedInstance("OrganizationService", "OrganizationService");
        Assert.Equal(ServiceExecutionScope.Shared, shared.ExecutionScope);
        Assert.Null(shared.WorkspaceId);
        Assert.Equal("Unassigned", shared.PortDisplay);
    }

    [Fact]
    public void WorkspaceInstance_Cursor_And_Claude_Supported()
    {
        var cursor = service.EnsureWorkspaceInstance("OrganizationService", "OrganizationService", "cursor");
        var claude = service.EnsureWorkspaceInstance("OrganizationService", "OrganizationService", "claude");
        Assert.Equal(ServiceExecutionScope.Workspace, cursor.ExecutionScope);
        Assert.Equal("cursor", cursor.WorkspaceId);
        Assert.Equal("claude", claude.WorkspaceId);
        Assert.NotEqual(cursor.InstanceId, claude.InstanceId);
    }

    [Fact]
    public async Task MultipleInstances_Coexist_SharedCursorClaude()
    {
        service.EnsureSharedInstance("OrganizationService", "OrganizationService");
        service.EnsureWorkspaceInstance("OrganizationService", "OrganizationService", "cursor");
        service.EnsureWorkspaceInstance("OrganizationService", "OrganizationService", "claude");
        await service.RefreshAsync();

        var forService = service.GetInstancesForService("OrganizationService");
        Assert.Equal(3, forService.Count);
        Assert.Contains(forService, i => i.ExecutionScope == ServiceExecutionScope.Shared);
        Assert.Contains(forService, i => i.WorkspaceId == "cursor");
        Assert.Contains(forService, i => i.WorkspaceId == "claude");
    }

    [Fact]
    public async Task Shared_UsesRuntimeTruth_DoesNotFabricatePortOrPid()
    {
        service.EnsureSharedInstance("OrganizationService", "OrganizationService");
        await service.RefreshAsync();
        var shared = service.GetInstancesForService("OrganizationService")
            .Single(i => i.ExecutionScope == ServiceExecutionScope.Shared);
        Assert.Equal(ServiceInstanceState.Stopped, shared.State);
        Assert.Equal("Unassigned", shared.PortDisplay);
        Assert.Equal("—", shared.ProcessIdDisplay);
    }

    [Fact]
    public async Task Shared_Running_MapsKnownPortAndPid()
    {
        host.Services[0].Status = ServiceStatus.Running;
        host.Services[0].HttpsPort = 7217;
        host.Services[0].ProcessId = 4242;
        service.EnsureSharedInstance("OrganizationService", "OrganizationService");
        await service.RefreshAsync();
        var shared = service.GetInstancesForService("OrganizationService")
            .Single(i => i.ExecutionScope == ServiceExecutionScope.Shared);
        Assert.Equal(ServiceInstanceState.Running, shared.State);
        Assert.Equal("7217", shared.PortDisplay);
        Assert.Equal("4242", shared.ProcessIdDisplay);
    }

    [Fact]
    public async Task WorkspaceInstance_DoesNotFabricateRunningState()
    {
        host.Services[0].Status = ServiceStatus.Running;
        host.Services[0].HttpsPort = 7217;
        host.Services[0].ProcessId = 4242;
        service.EnsureWorkspaceInstance("OrganizationService", "OrganizationService", "cursor");
        await service.RefreshAsync();
        var cursor = service.GetInstancesForService("OrganizationService")
            .Single(i => i.WorkspaceId == "cursor");
        Assert.Equal(ServiceInstanceState.Defined, cursor.State);
        Assert.Equal("Unassigned", cursor.PortDisplay);
        Assert.Equal("—", cursor.ProcessIdDisplay);
        Assert.False(string.IsNullOrWhiteSpace(cursor.Branch));
    }

    [Fact]
    public async Task MissingWorkspace_BecomesUnavailable()
    {
        service.EnsureWorkspaceInstance("OrganizationService", "OrganizationService", "missing-ws");
        await service.RefreshAsync();
        var missing = service.GetInstancesForService("OrganizationService")
            .Single(i => i.WorkspaceId == "missing-ws");
        Assert.Equal(ServiceInstanceState.Unavailable, missing.State);
        Assert.Contains(missing.AttentionReasons, r => r.Contains("missing", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Persistence_RoundTrip()
    {
        var created = service.EnsureSharedInstance("Gateway", "Gateway");
        var registry = new ServiceInstanceRegistry(storePath);
        var loaded = registry.GetById(created.InstanceId);
        Assert.NotNull(loaded);
        Assert.Equal("Gateway", loaded!.ServiceId);
        Assert.Equal(ServiceExecutionScope.Shared, loaded.ExecutionScope);
    }

    [Fact]
    public void NotConfiguredPlaceholders_ForAgentWorkspacesWithoutInstance()
    {
        service.EnsureSharedInstance("OrganizationService", "OrganizationService");
        var placeholders = service.GetNotConfiguredPlaceholders("OrganizationService", "OrganizationService");
        Assert.Contains(placeholders, p => p.WorkspaceId == "cursor" && !p.IsConfigured);
        Assert.Contains(placeholders, p => p.WorkspaceId == "claude" && !p.IsConfigured);
        Assert.All(placeholders, p => Assert.Equal("Not Configured", p.StateDisplay));
    }

    [Fact]
    public void InstanceIds_AreUnique_AcrossSameService()
    {
        var a = service.EnsureSharedInstance("OrganizationService", "OrganizationService");
        var b = service.EnsureWorkspaceInstance("OrganizationService", "OrganizationService", "cursor");
        Assert.NotEqual(a.InstanceId, b.InstanceId);
    }

    [Fact]
    public void AgentSession_And_Profile_Association_Stored()
    {
        var item = service.EnsureWorkspaceInstance(
            "OrganizationService",
            "OrganizationService",
            "cursor",
            agentSessionId: "sess-1",
            runtimeProfileId: "profile-1");
        Assert.Equal("sess-1", item.AgentSessionId);
        Assert.Equal("profile-1", item.RuntimeProfileId);
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

    private sealed class FakeServiceRuntimeHost : IServiceRuntimeHost
    {
        public ObservableCollection<ServiceInfoModel> Services { get; } = [];

        public Task StartAsync(ServiceInfoModel service, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task StopAsync(ServiceInfoModel service, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class NoopLogger : ILoggingService
    {
        public ObservableCollection<LogEntryModel> Entries { get; } = [];

        public void Log(LogLevel level, string source, string message)
        {
        }

        public void Info(string source, string message)
        {
        }

        public void Warning(string source, string message)
        {
        }

        public void Error(string source, string message)
        {
        }

        public void Build(string source, string message)
        {
        }

        public void Process(string source, string message)
        {
        }
    }
}
