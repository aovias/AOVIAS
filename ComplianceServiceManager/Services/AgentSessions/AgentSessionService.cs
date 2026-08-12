using System.IO;
using ComplianceServiceManager.Logging;
using ComplianceServiceManager.Models.AgentSessions;
using ComplianceServiceManager.Models.LocalPullRequests;
using ComplianceServiceManager.Models.LocalResources;
using ComplianceServiceManager.Models.Workspaces;
using ComplianceServiceManager.Services.LocalPullRequests;
using ComplianceServiceManager.Services.LocalResources;
using ComplianceServiceManager.Services.RuntimeProfiles;
using ComplianceServiceManager.Services.Workspaces;

namespace ComplianceServiceManager.Services.AgentSessions;

/// <summary>
/// Agent-aware development session layer. Observes and coordinates metadata only.
/// </summary>
public sealed class AgentSessionService : IAgentSessionService
{
    private readonly AgentSessionRegistry registry;
    private readonly DevelopmentWorkspaceRegistry workspaces;
    private readonly IGitWorkspaceService git;
    private readonly IRuntimeProfileOrchestrator? profiles;
    private readonly ILocalPullRequestService? pullRequests;
    private readonly ILocalResourceMonitoringService? resources;
    private readonly ILoggingService logger;
    private readonly object gate = new();
    private List<AgentSessionItem> items = [];

    public AgentSessionService(
        AgentSessionRegistry registry,
        DevelopmentWorkspaceRegistry workspaces,
        IGitWorkspaceService git,
        ILoggingService logger,
        IRuntimeProfileOrchestrator? profiles = null,
        ILocalPullRequestService? pullRequests = null,
        ILocalResourceMonitoringService? resources = null)
    {
        this.registry = registry;
        this.workspaces = workspaces;
        this.git = git;
        this.logger = logger;
        this.profiles = profiles;
        this.pullRequests = pullRequests;
        this.resources = resources;
        ReloadFromStore();
    }

    public event Action? SessionsChanged;

    public IReadOnlyList<AgentSessionItem> GetSessions()
    {
        lock (gate)
        {
            return items.ToList();
        }
    }

    public IReadOnlyList<(string Id, string DisplayName, string Path, DevelopmentAgentType? PreferredAgent)> GetWorkspaceChoices()
        => workspaces.GetEnabledWorkspaces()
            .Where(workspace => !workspace.IsMain)
            .Select(workspace => (
                workspace.Id,
                workspace.DisplayName,
                workspace.Path,
                MapPreferredAgent(workspace.AgentOwner)))
            .ToList();

    public IReadOnlyList<(string Id, string Name)> GetRuntimeProfileChoices()
        => profiles?.GetProfiles().Select(profile => (profile.Id, profile.Name)).ToList()
           ?? [];

    public IReadOnlyList<(string Id, string Title)> GetPullRequestChoices()
        => pullRequests?.GetPullRequests().Select(pr => (pr.Id, pr.Title)).ToList()
           ?? [];

    public async Task RefreshAllAsync(CancellationToken cancellationToken = default)
    {
        foreach (var session in GetSessions())
        {
            cancellationToken.ThrowIfCancellationRequested();
            await RefreshAsync(session, cancellationToken);
        }

        SessionsChanged?.Invoke();
    }

    public async Task RefreshAsync(AgentSessionItem session, CancellationToken cancellationToken = default)
    {
        var reasons = new List<string>();
        var workspace = workspaces.GetEnabledWorkspaces()
            .FirstOrDefault(item => string.Equals(item.Id, session.WorkspaceId, StringComparison.OrdinalIgnoreCase));

        if (workspace is null)
        {
            session.WorkspaceDisplay = session.WorkspaceId;
            session.WorkspacePath = string.Empty;
            session.ActualBranch = null;
            reasons.Add("Workspace missing from Studio configuration.");
            ApplyTerminalOrStale(session, reasons);
            Persist(session);
            return;
        }

        session.WorkspaceDisplay = workspace.DisplayName;
        session.WorkspacePath = workspace.Path;

        if (!Directory.Exists(workspace.Path) || !git.LooksLikeGitWorktree(workspace.Path))
        {
            session.ActualBranch = null;
            reasons.Add("Workspace path is missing or is not a valid Git worktree.");
            ApplyTerminalOrStale(session, reasons);
            Persist(session);
            return;
        }

        var state = await git.GetWorkspaceStateAsync(workspace.Path, cancellationToken);
        session.ActualBranch = state.Branch;
        session.LastObservedAt = DateTimeOffset.UtcNow;

        if (!state.IsAccessible)
        {
            reasons.Add(state.Error ?? "Git workspace is inaccessible.");
        }
        else if (string.IsNullOrWhiteSpace(state.Branch))
        {
            reasons.Add("Unable to determine current branch.");
        }
        else if (!string.Equals(state.Branch, session.ExpectedBranch, StringComparison.OrdinalIgnoreCase))
        {
            reasons.Add($"Branch mismatch. Expected '{session.ExpectedBranch}', actual '{state.Branch}'.");
        }

        if (!string.IsNullOrWhiteSpace(session.RuntimeProfileId))
        {
            var profile = profiles?.GetProfiles()
                .FirstOrDefault(item => string.Equals(item.Id, session.RuntimeProfileId, StringComparison.OrdinalIgnoreCase));
            if (profile is null)
            {
                session.RuntimeProfileDisplay = session.RuntimeProfileId;
                reasons.Add("Runtime profile is missing.");
            }
            else
            {
                session.RuntimeProfileDisplay = profile.Name;
            }
        }
        else
        {
            session.RuntimeProfileDisplay = "—";
        }

        if (!string.IsNullOrWhiteSpace(session.PullRequestId))
        {
            var pr = pullRequests?.GetPullRequests()
                .FirstOrDefault(item => string.Equals(item.Id, session.PullRequestId, StringComparison.OrdinalIgnoreCase));
            if (pr is null)
            {
                session.PullRequestDisplay = session.PullRequestId;
                reasons.Add("Local PR is missing.");
            }
            else
            {
                session.PullRequestDisplay = $"{pr.Title} · {pr.StatusDisplay}";
            }
        }
        else
        {
            session.PullRequestDisplay = "—";
        }

        session.ResourcesDisplay = FormatResources(resources?.LatestSnapshot);

        if (reasons.Count == 0)
        {
            session.ApplyHealth(AgentSessionHealth.Healthy, []);
            if (session.Status is AgentSessionStatus.Stale or AgentSessionStatus.Blocked)
            {
                // Recover metadata status only when previously attention-flagged by reconciliation.
                session.ApplyStatus(session.StartedAt is null ? AgentSessionStatus.Planned : AgentSessionStatus.Active);
            }
        }
        else
        {
            session.ApplyHealth(AgentSessionHealth.NeedsAttention, reasons);
            if (session.Status is not (AgentSessionStatus.Completed or AgentSessionStatus.Cancelled))
            {
                session.ApplyStatus(AgentSessionStatus.Stale);
            }
        }

        session.SummaryDisplay =
            $"{session.AgentDisplay} · {session.WorkspaceDisplay} · {session.ExpectedBranch}";
        Persist(session);
    }

    public async Task<AgentSessionItem> CreateAsync(
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
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new InvalidOperationException("Session title is required.");
        }

        if (string.IsNullOrWhiteSpace(workspaceId))
        {
            throw new InvalidOperationException("Workspace is required.");
        }

        if (string.IsNullOrWhiteSpace(expectedBranch))
        {
            throw new InvalidOperationException("Expected branch is required.");
        }

        var workspace = workspaces.GetEnabledWorkspaces()
            .FirstOrDefault(item => string.Equals(item.Id, workspaceId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("Workspace is not registered.");

        if (workspace.IsMain)
        {
            throw new InvalidOperationException("Cannot bind a development-agent session to the main/integration workspace.");
        }

        EnsureWorkspaceOwnership(workspaceId, allowWorkspaceOverride);

        var item = new AgentSessionItem
        {
            Id = Guid.NewGuid().ToString("N")[..12],
            AgentType = agentType,
            AgentDisplay = agentType.ToString(),
            Title = title.Trim(),
            Description = description?.Trim() ?? string.Empty,
            TaskId = string.IsNullOrWhiteSpace(taskId) ? null : taskId.Trim(),
            WorkspaceId = workspace.Id,
            WorkspaceDisplay = workspace.DisplayName,
            WorkspacePath = workspace.Path,
            ExpectedBranch = expectedBranch.Trim(),
            RuntimeProfileId = string.IsNullOrWhiteSpace(runtimeProfileId) ? null : runtimeProfileId.Trim(),
            PullRequestId = string.IsNullOrWhiteSpace(pullRequestId) ? null : pullRequestId.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        item.ApplyStatus(AgentSessionStatus.Planned);

        lock (gate)
        {
            items.Add(item);
        }

        Persist(item);
        await RefreshAsync(item, cancellationToken);
        logger.Info("AgentSessions", $"Created {agentType} session '{item.Title}' ({item.Id}) on {item.ExpectedBranch}");
        SessionsChanged?.Invoke();
        return item;
    }

    public async Task StartAsync(AgentSessionItem session, CancellationToken cancellationToken = default)
    {
        if (session.Status is AgentSessionStatus.Completed or AgentSessionStatus.Cancelled)
        {
            throw new InvalidOperationException("Cannot start a completed or cancelled session.");
        }

        EnsureWorkspaceOwnership(session.WorkspaceId, allowOverride: false, excludingSessionId: session.Id);
        await RefreshAsync(session, cancellationToken);
        if (session.Health == AgentSessionHealth.NeedsAttention)
        {
            session.ApplyStatus(AgentSessionStatus.Blocked);
            Persist(session);
            SessionsChanged?.Invoke();
            throw new InvalidOperationException(
                "Session cannot start while workspace/branch validation needs attention: "
                + string.Join("; ", session.AttentionReasons));
        }

        session.StartedAt ??= DateTimeOffset.UtcNow;
        session.LastObservedAt = DateTimeOffset.UtcNow;
        session.ApplyStatus(AgentSessionStatus.Active);
        Persist(session);
        SessionsChanged?.Invoke();
    }

    public Task PauseAsync(AgentSessionItem session)
    {
        if (session.Status != AgentSessionStatus.Active)
        {
            throw new InvalidOperationException("Only ACTIVE sessions can be paused.");
        }

        session.ApplyStatus(AgentSessionStatus.Paused);
        Persist(session);
        SessionsChanged?.Invoke();
        return Task.CompletedTask;
    }

    public async Task ResumeAsync(AgentSessionItem session, CancellationToken cancellationToken = default)
    {
        if (session.Status != AgentSessionStatus.Paused)
        {
            throw new InvalidOperationException("Only PAUSED sessions can be resumed.");
        }

        await StartAsync(session, cancellationToken);
    }

    public Task CompleteAsync(AgentSessionItem session)
    {
        if (session.Status is not (AgentSessionStatus.Active or AgentSessionStatus.Paused or AgentSessionStatus.Stale or AgentSessionStatus.Blocked))
        {
            throw new InvalidOperationException("Session cannot be completed from its current state.");
        }

        session.CompletedAt = DateTimeOffset.UtcNow;
        session.ApplyStatus(AgentSessionStatus.Completed);
        Persist(session);
        SessionsChanged?.Invoke();
        return Task.CompletedTask;
    }

    public Task CancelAsync(AgentSessionItem session)
    {
        if (session.Status is AgentSessionStatus.Completed)
        {
            throw new InvalidOperationException("Completed sessions cannot be cancelled.");
        }

        session.CompletedAt = DateTimeOffset.UtcNow;
        session.ApplyStatus(AgentSessionStatus.Cancelled);
        Persist(session);
        SessionsChanged?.Invoke();
        return Task.CompletedTask;
    }

    private void EnsureWorkspaceOwnership(string workspaceId, bool allowOverride, string? excludingSessionId = null)
    {
        var conflict = GetSessions().FirstOrDefault(session =>
            !string.Equals(session.Id, excludingSessionId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(session.WorkspaceId, workspaceId, StringComparison.OrdinalIgnoreCase)
            && session.Status is AgentSessionStatus.Active or AgentSessionStatus.Paused or AgentSessionStatus.Planned or AgentSessionStatus.Stale or AgentSessionStatus.Blocked);

        if (conflict is null)
        {
            return;
        }

        if (!allowOverride)
        {
            throw new InvalidOperationException(
                $"WORKSPACE ALREADY IN USE by {conflict.AgentDisplay} session '{conflict.Title}' ({conflict.StatusDisplay}).");
        }
    }

    private void ApplyTerminalOrStale(AgentSessionItem session, List<string> reasons)
    {
        session.ApplyHealth(AgentSessionHealth.NeedsAttention, reasons);
        if (session.Status is not (AgentSessionStatus.Completed or AgentSessionStatus.Cancelled))
        {
            session.ApplyStatus(AgentSessionStatus.Stale);
        }
    }

    private static string FormatResources(LocalResourceSnapshot? snapshot)
    {
        if (snapshot?.Host is null)
        {
            return "—";
        }

        var host = snapshot.Host;
        var cpu = host.CpuPercent is null ? "—" : $"{host.CpuPercent:0.#}%";
        var ram = host.MemoryUsedBytes <= 0
            ? "—"
            : LocalResourceFormatting.FormatBytes(host.MemoryUsedBytes);
        return $"CPU {cpu} · RAM {ram}";
    }

    private void ReloadFromStore()
    {
        lock (gate)
        {
            items = registry.GetAll().Select(AgentSessionItem.FromDefinition).ToList();
        }
    }

    private void Persist(AgentSessionItem item)
    {
        item.UpdatedAt = DateTimeOffset.UtcNow;
        registry.Upsert(item.ToDefinition());
    }

    private static DevelopmentAgentType? MapPreferredAgent(WorkspaceAgentOwner owner)
        => owner switch
        {
            WorkspaceAgentOwner.Cursor => DevelopmentAgentType.Cursor,
            WorkspaceAgentOwner.Claude => DevelopmentAgentType.Claude,
            _ => null
        };
}
