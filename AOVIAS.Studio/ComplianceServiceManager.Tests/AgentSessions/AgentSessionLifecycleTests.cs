using System.Collections.ObjectModel;
using ComplianceServiceManager.Logging;
using ComplianceServiceManager.Models;
using ComplianceServiceManager.Models.AgentSessions;
using ComplianceServiceManager.Models.Workspaces;
using ComplianceServiceManager.Services.AgentSessions;
using ComplianceServiceManager.Services.Workspaces;

namespace ComplianceServiceManager.Tests.AgentSessions;

public sealed class AgentSessionLifecycleTests : IDisposable
{
    private readonly string root;
    private readonly string storePath;
    private readonly string workspaceStore;
    private readonly FakeGit git;
    private readonly AgentSessionService service;

    public AgentSessionLifecycleTests()
    {
        root = Path.Combine(Path.GetTempPath(), "aovias-sessions-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var cursor = Path.Combine(root, "AOVIAS.Cursor");
        var claude = Path.Combine(root, "AOVIAS.Claude");
        var main = Path.Combine(root, "AOVIAS");
        Directory.CreateDirectory(cursor);
        Directory.CreateDirectory(claude);
        Directory.CreateDirectory(main);
        File.WriteAllText(Path.Combine(cursor, ".git"), "gitdir: ../AOVIAS/.git/worktrees/cursor");
        File.WriteAllText(Path.Combine(claude, ".git"), "gitdir: ../AOVIAS/.git/worktrees/claude");
        Directory.CreateDirectory(Path.Combine(main, ".git"));

        storePath = Path.Combine(root, "agent-sessions.json");
        workspaceStore = Path.Combine(root, "development-workspaces.json");
        File.WriteAllText(workspaceStore, System.Text.Json.JsonSerializer.Serialize(
            DevelopmentWorkspaceRegistry.BuildDefaults(root),
            new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            }));

        git = new FakeGit
        {
            Branches =
            {
                [cursor] = "agent/cursor/task-a",
                [claude] = "agent/claude/active"
            }
        };

        service = new AgentSessionService(
            new AgentSessionRegistry(storePath),
            new DevelopmentWorkspaceRegistry(workspaceStore),
            git,
            new NoopLogger());
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(root, true);
        }
        catch
        {
        }
    }

    [Fact]
    public async Task Create_Persist_Load()
    {
        var created = await service.CreateAsync(
            DevelopmentAgentType.Cursor,
            "Command Onboarding",
            "desc",
            "AOVIAS-CMD-ONBOARDING",
            "cursor",
            "agent/cursor/task-a",
            null,
            null);

        Assert.Equal(AgentSessionStatus.Planned, created.Status);
        Assert.Equal(AgentSessionHealth.Healthy, created.Health);

        var reloaded = new AgentSessionRegistry(storePath).GetAll();
        Assert.Contains(reloaded, item => item.Id == created.Id && item.Title == "Command Onboarding");
    }

    [Fact]
    public async Task Start_Pause_Resume_Complete()
    {
        var session = await service.CreateAsync(
            DevelopmentAgentType.Cursor,
            "Lifecycle",
            "",
            null,
            "cursor",
            "agent/cursor/task-a",
            null,
            null);

        await service.StartAsync(session);
        Assert.Equal(AgentSessionStatus.Active, session.Status);
        Assert.NotNull(session.StartedAt);

        await service.PauseAsync(session);
        Assert.Equal(AgentSessionStatus.Paused, session.Status);

        await service.ResumeAsync(session);
        Assert.Equal(AgentSessionStatus.Active, session.Status);

        await service.CompleteAsync(session);
        Assert.Equal(AgentSessionStatus.Completed, session.Status);
        Assert.NotNull(session.CompletedAt);
    }

    [Fact]
    public async Task Cancel_FromPlanned()
    {
        var session = await service.CreateAsync(
            DevelopmentAgentType.Claude,
            "CancelMe",
            "",
            "P8-CL02",
            "claude",
            "agent/claude/active",
            null,
            null);
        await service.CancelAsync(session);
        Assert.Equal(AgentSessionStatus.Cancelled, session.Status);
    }

    [Fact]
    public async Task BranchMismatch_NeedsAttention()
    {
        var session = await service.CreateAsync(
            DevelopmentAgentType.Cursor,
            "Mismatch",
            "",
            null,
            "cursor",
            "agent/cursor/expected",
            null,
            null);

        Assert.Equal(AgentSessionHealth.NeedsAttention, session.Health);
        Assert.Contains(session.AttentionReasons, reason => reason.Contains("Branch mismatch", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(AgentSessionStatus.Stale, session.Status);
    }

    [Fact]
    public async Task MissingWorkspace_NeedsAttention()
    {
        var session = await service.CreateAsync(
            DevelopmentAgentType.Cursor,
            "Temp",
            "",
            null,
            "cursor",
            "agent/cursor/task-a",
            null,
            null);

        // Corrupt workspace id after create.
        session.WorkspaceId = "missing-workspace";
        await service.RefreshAsync(session);
        Assert.Equal(AgentSessionHealth.NeedsAttention, session.Health);
        Assert.Contains(session.AttentionReasons, reason => reason.Contains("Workspace missing", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task WorkspaceOwnership_Conflict()
    {
        await service.CreateAsync(
            DevelopmentAgentType.Cursor,
            "Owner",
            "",
            null,
            "cursor",
            "agent/cursor/task-a",
            null,
            null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(
            DevelopmentAgentType.Cursor,
            "Intruder",
            "",
            null,
            "cursor",
            "agent/cursor/task-a",
            null,
            null));
        Assert.Contains("WORKSPACE ALREADY IN USE", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MultipleSessions_DifferentWorkspaces()
    {
        var cursor = await service.CreateAsync(
            DevelopmentAgentType.Cursor,
            "Cursor Task",
            "",
            null,
            "cursor",
            "agent/cursor/task-a",
            null,
            null);
        var claude = await service.CreateAsync(
            DevelopmentAgentType.Claude,
            "Claude Task",
            "",
            "P8-CL02",
            "claude",
            "agent/claude/active",
            null,
            null);

        Assert.Equal(2, service.GetSessions().Count);
        Assert.NotEqual(cursor.WorkspaceId, claude.WorkspaceId);
    }

    [Fact]
    public async Task Start_BlockedWhenUnhealthy()
    {
        var session = await service.CreateAsync(
            DevelopmentAgentType.Cursor,
            "BadStart",
            "",
            null,
            "cursor",
            "agent/cursor/wrong",
            null,
            null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.StartAsync(session));
        Assert.Contains("needs attention", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(AgentSessionStatus.Blocked, session.Status);
    }

    [Fact]
    public async Task MissingRuntimeProfile_Reported()
    {
        var session = await service.CreateAsync(
            DevelopmentAgentType.Cursor,
            "Profile",
            "",
            null,
            "cursor",
            "agent/cursor/task-a",
            "missing-profile",
            null);
        Assert.Contains(session.AttentionReasons, reason => reason.Contains("Runtime profile", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task MissingPullRequest_Reported()
    {
        var session = await service.CreateAsync(
            DevelopmentAgentType.Cursor,
            "PR",
            "",
            null,
            "cursor",
            "agent/cursor/task-a",
            null,
            "missing-pr");
        Assert.Contains(session.AttentionReasons, reason => reason.Contains("Local PR", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class FakeGit : IGitWorkspaceService
    {
        public Dictionary<string, string> Branches { get; } = new(StringComparer.OrdinalIgnoreCase);

        public bool LooksLikeGitWorktree(string path)
            => File.Exists(Path.Combine(path, ".git")) || Directory.Exists(Path.Combine(path, ".git"));

        public Task<IReadOnlyList<GitWorktreeInfo>> ListWorktreesAsync(string repositoryOrWorktreePath, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<GitWorktreeInfo>>([]);

        public Task<GitWorkspaceState> GetWorkspaceStateAsync(string worktreePath, CancellationToken cancellationToken = default)
        {
            Branches.TryGetValue(worktreePath, out var branch);
            return Task.FromResult(new GitWorkspaceState
            {
                Path = worktreePath,
                IsAccessible = true,
                Branch = branch,
                Head = "abcdef1",
                IsClean = true
            });
        }
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
