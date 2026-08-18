using System.IO;
using ComplianceServiceManager.Models.SourceContext;
using ComplianceServiceManager.Models.Workspaces;
using ComplianceServiceManager.Services.WorkspaceRuntime;
using ComplianceServiceManager.Services.Workspaces;

namespace ComplianceServiceManager.Services.SourceContexts;

/// <summary>
/// Resolves Shared, Cursor, and Claude source identities from D1 workspace registry
/// plus read-only Git observation. Never mutates Git.
/// </summary>
public sealed class SourceContextService : ISourceContextService
{
    private readonly DevelopmentWorkspaceRegistry workspaceRegistry;
    private readonly IGitWorkspaceService gitWorkspaceService;

    public SourceContextService(
        DevelopmentWorkspaceRegistry workspaceRegistry,
        IGitWorkspaceService gitWorkspaceService)
    {
        this.workspaceRegistry = workspaceRegistry;
        this.gitWorkspaceService = gitWorkspaceService;
    }

    public async Task<SourceContext> ResolveAsync(string contextId, CancellationToken cancellationToken = default)
    {
        var all = await ResolveAllAsync(cancellationToken);
        var match = all.FirstOrDefault(item =>
            string.Equals(item.ContextId, contextId, StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            throw new InvalidOperationException($"Unknown source context '{contextId}'.");
        }

        return match;
    }

    public async Task<IReadOnlyList<SourceContext>> ResolveAllAsync(CancellationToken cancellationToken = default)
    {
        var contexts = new List<SourceContext>();
        foreach (var pair in workspaceRegistry.GetEnabledWorkspacePairs())
        {
            cancellationToken.ThrowIfCancellationRequested();
            contexts.Add(await BuildContextAsync(pair.Workspace, cancellationToken));
        }

        return contexts;
    }

    public bool AcceptsSourcePath(SourceContext context, string path)
    {
        var shared = workspaceRegistry.GetEnabledWorkspaces().FirstOrDefault(item => item.IsMain)
                     ?? workspaceRegistry.GetEnabledWorkspaces().FirstOrDefault(item =>
                         string.Equals(item.Id, SourceContextIds.Shared, StringComparison.OrdinalIgnoreCase)
                         || string.Equals(item.Id, "main", StringComparison.OrdinalIgnoreCase));
        var cursor = workspaceRegistry.GetEnabledWorkspaces().FirstOrDefault(item =>
            string.Equals(item.Id, SourceContextIds.Cursor, StringComparison.OrdinalIgnoreCase));
        var claude = workspaceRegistry.GetEnabledWorkspaces().FirstOrDefault(item =>
            string.Equals(item.Id, SourceContextIds.Claude, StringComparison.OrdinalIgnoreCase));

        return SourceContextValidator.AcceptsSourcePath(
            context,
            path,
            ToPathOnly(SourceContextIds.Shared, shared),
            ToPathOnly(SourceContextIds.Cursor, cursor),
            ToPathOnly(SourceContextIds.Claude, claude));
    }

    public SourceContext? TryResolveContextForPath(string path, IReadOnlyList<SourceContext>? cached = null)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var full = Path.GetFullPath(path);
        var contexts = cached ?? BuildPathOnlyContexts();
        SourceContext? best = null;
        var bestLength = -1;

        foreach (var context in contexts)
        {
            if (!SourceContextValidator.IsUnderRoot(full, context.RootPath))
            {
                continue;
            }

            if (context.RootPath.Length > bestLength)
            {
                best = context;
                bestLength = context.RootPath.Length;
            }
        }

        return best;
    }

    private async Task<SourceContext> BuildContextAsync(ConfiguredWorkspace workspace, CancellationToken cancellationToken)
    {
        var contextId = MapContextId(workspace);
        var kind = workspace.IsMain ? SourceContextKind.Shared : SourceContextKind.Workspace;
        var rootPath = string.IsNullOrWhiteSpace(workspace.Path)
            ? string.Empty
            : Path.GetFullPath(workspace.Path);

        if (!Directory.Exists(rootPath) || !gitWorkspaceService.LooksLikeGitWorktree(rootPath))
        {
            return new SourceContext
            {
                ContextId = contextId,
                Kind = kind,
                WorkspaceId = workspace.IsMain ? null : workspace.Id,
                RootPath = rootPath,
                Agent = workspace.AgentOwner,
                Ownership = SourceContextValidator.ResolveOwnership(kind, workspace.AgentOwner),
                GitValidity = SourceContextGitValidity.Unavailable,
                GitStatusReason = string.IsNullOrWhiteSpace(rootPath)
                    ? "Workspace path is missing."
                    : "Workspace path is not accessible or is not a Git worktree.",
                ObservedAt = DateTimeOffset.UtcNow
            };
        }

        var state = await gitWorkspaceService.GetWorkspaceStateAsync(rootPath, cancellationToken);
        var validity = SourceContextValidator.EvaluateGitValidity(
            kind,
            workspace.IsMain ? null : workspace.Id,
            state.IsAccessible,
            state.IsDetached,
            state.Branch);

        return new SourceContext
        {
            ContextId = contextId,
            Kind = kind,
            WorkspaceId = workspace.IsMain ? null : workspace.Id,
            RootPath = rootPath,
            Branch = state.Branch,
            Head = state.Head,
            Agent = workspace.AgentOwner,
            Ownership = SourceContextValidator.ResolveOwnership(kind, workspace.AgentOwner),
            GitValidity = validity,
            GitStatusReason = validity == SourceContextGitValidity.Accessible
                ? null
                : state.Error ?? DescribeInvalidity(validity, state.Branch, workspace.Id),
            ObservedAt = state.ObservedAt
        };
    }

    private static string MapContextId(ConfiguredWorkspace workspace)
    {
        if (workspace.IsMain || string.Equals(workspace.Id, "main", StringComparison.OrdinalIgnoreCase))
        {
            return SourceContextIds.Shared;
        }

        return workspace.Id;
    }

    private static string DescribeInvalidity(SourceContextGitValidity validity, string? branch, string workspaceId)
        => validity switch
        {
            SourceContextGitValidity.Detached => "HEAD is detached.",
            SourceContextGitValidity.InvalidBranch when string.Equals(workspaceId, SourceContextIds.Shared, StringComparison.OrdinalIgnoreCase)
                                                       || string.Equals(workspaceId, "main", StringComparison.OrdinalIgnoreCase)
                => $"Expected branch 'main' but observed '{branch ?? "(none)"}'.",
            SourceContextGitValidity.InvalidBranch
                => $"Branch '{branch ?? "(none)"}' does not match expected prefix '{WorkspaceSourceMapper.ExpectedBranchPrefix(workspaceId)}'.",
            _ => "Git repository cannot be accessed."
        };

    private IReadOnlyList<SourceContext> BuildPathOnlyContexts()
    {
        var list = new List<SourceContext>();
        foreach (var workspace in workspaceRegistry.GetEnabledWorkspaces())
        {
            list.Add(ToPathOnly(MapContextId(workspace), workspace));
        }

        return list;
    }

    private static SourceContext ToPathOnly(string contextId, ConfiguredWorkspace? workspace)
    {
        var kind = workspace?.IsMain == true ? SourceContextKind.Shared : SourceContextKind.Workspace;
        var rootPath = string.IsNullOrWhiteSpace(workspace?.Path)
            ? string.Empty
            : Path.GetFullPath(workspace.Path);

        return new SourceContext
        {
            ContextId = contextId,
            Kind = kind,
            WorkspaceId = workspace?.IsMain == true ? null : workspace?.Id,
            RootPath = rootPath,
            Agent = workspace?.AgentOwner ?? WorkspaceAgentOwner.Unknown,
            Ownership = SourceContextValidator.ResolveOwnership(kind, workspace?.AgentOwner ?? WorkspaceAgentOwner.Unknown),
            GitValidity = Directory.Exists(rootPath)
                ? SourceContextGitValidity.Accessible
                : SourceContextGitValidity.Unavailable
        };
    }
}
