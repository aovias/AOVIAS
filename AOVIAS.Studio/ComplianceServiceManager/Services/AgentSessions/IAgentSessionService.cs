using ComplianceServiceManager.Models.AgentSessions;

namespace ComplianceServiceManager.Services.AgentSessions;

/// <summary>
/// Development-agent session orchestration metadata.
/// Does not control Cursor/Claude internals, mutate Git, or start/stop runtimes automatically.
/// </summary>
public interface IAgentSessionService
{
    IReadOnlyList<AgentSessionItem> GetSessions();

    event Action? SessionsChanged;

    Task RefreshAllAsync(CancellationToken cancellationToken = default);

    Task RefreshAsync(AgentSessionItem session, CancellationToken cancellationToken = default);

    Task<AgentSessionItem> CreateAsync(
        DevelopmentAgentType agentType,
        string title,
        string description,
        string? taskId,
        string workspaceId,
        string expectedBranch,
        string? runtimeProfileId,
        string? pullRequestId,
        bool allowWorkspaceOverride = false,
        CancellationToken cancellationToken = default);

    Task StartAsync(AgentSessionItem session, CancellationToken cancellationToken = default);

    Task PauseAsync(AgentSessionItem session);

    Task ResumeAsync(AgentSessionItem session, CancellationToken cancellationToken = default);

    Task CompleteAsync(AgentSessionItem session);

    Task CancelAsync(AgentSessionItem session);

    IReadOnlyList<(string Id, string DisplayName, string Path, DevelopmentAgentType? PreferredAgent)> GetWorkspaceChoices();

    IReadOnlyList<(string Id, string Name)> GetRuntimeProfileChoices();

    IReadOnlyList<(string Id, string Title)> GetPullRequestChoices();
}
