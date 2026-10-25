using System.IO;
using ComplianceServiceManager.Logging;
using ComplianceServiceManager.Models.Workspaces;

namespace ComplianceServiceManager.Services.Workspaces;

/// <summary>
/// Loads configured AOVIAS development workspaces and refreshes derived Git state.
/// Configuration owns display name / agent owner / role; Git owns branch / HEAD / dirty.
/// </summary>
public sealed class DevelopmentWorkspaceService : IDevelopmentWorkspaceService
{
    private readonly DevelopmentWorkspaceRegistry registry;
    private readonly IGitWorkspaceService gitWorkspaceService;
    private readonly ILoggingService logger;

    public DevelopmentWorkspaceService(
        DevelopmentWorkspaceRegistry registry,
        IGitWorkspaceService gitWorkspaceService,
        ILoggingService logger)
    {
        this.registry = registry;
        this.gitWorkspaceService = gitWorkspaceService;
        this.logger = logger;
    }

    public async Task<IReadOnlyList<DevelopmentWorkspaceItem>> RefreshAsync(CancellationToken cancellationToken = default)
    {
        var pairs = registry.GetEnabledWorkspacePairs();
        var items = new List<DevelopmentWorkspaceItem>(pairs.Count);

        foreach (var (repository, workspace) in pairs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var item = new DevelopmentWorkspaceItem
            {
                Id = workspace.Id,
                DisplayName = workspace.DisplayName,
                Path = workspace.Path,
                RepositoryId = repository.Id,
                RepositoryName = repository.Name,
                AgentOwner = workspace.AgentOwner,
                Role = workspace.Role,
                IsMain = workspace.IsMain
            };
            item.ApplyConfigurationDisplays();

            try
            {
                // Prefer listing worktrees from the repository root when available so linked
                // worktrees are recognized even if the configured path is temporarily missing.
                if (Directory.Exists(repository.RootPath) && gitWorkspaceService.LooksLikeGitWorktree(repository.RootPath))
                {
                    _ = await gitWorkspaceService.ListWorktreesAsync(repository.RootPath, cancellationToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.Warning("Workspaces", $"Worktree list for {repository.Name} failed: {ex.Message}");
            }

            var state = await gitWorkspaceService.GetWorkspaceStateAsync(workspace.Path, cancellationToken);
            item.ApplyGitState(state);
            if (!state.IsAccessible)
            {
                logger.Warning("Workspaces", $"{workspace.DisplayName}: {state.Error}");
            }

            items.Add(item);
        }

        return items;
    }
}
