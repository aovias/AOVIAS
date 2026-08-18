using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using ComplianceServiceManager.Logging;
using ComplianceServiceManager.Models;
using ComplianceServiceManager.Models.SourceContext;
using ComplianceServiceManager.Models.Workspaces;
using ComplianceServiceManager.Services;
using ComplianceServiceManager.Services.LocalDev;
using ComplianceServiceManager.Services.SourceContexts;
using ComplianceServiceManager.Services.Workspaces;

namespace ComplianceServiceManager.Tests.SourceContext;

public sealed class SourceContextTests : IDisposable
{
    private readonly string root;
    private readonly string mainPath;
    private readonly string cursorPath;
    private readonly string claudePath;
    private readonly string workspaceStore;
    private readonly FakeGitWorkspaceService git;
    private readonly DevelopmentWorkspaceRegistry workspaceRegistry;
    private readonly SourceContextService sourceContexts;
    private readonly SolutionFolderRegistry folderRegistry;
    private readonly SourceDiscoveryBoundary discoveryBoundary;
    private readonly ServiceDiscoveryService discoveryService;

    public SourceContextTests()
    {
        root = Path.Combine(Path.GetTempPath(), "aovias-sc-" + Guid.NewGuid().ToString("N"));
        mainPath = Path.Combine(root, "AOVIAS");
        cursorPath = Path.Combine(root, "AOVIAS.Cursor");
        claudePath = Path.Combine(root, "AOVIAS.Claude");
        Directory.CreateDirectory(mainPath);
        Directory.CreateDirectory(cursorPath);
        Directory.CreateDirectory(claudePath);
        Directory.CreateDirectory(Path.Combine(mainPath, "ComplaincePlatform"));
        Directory.CreateDirectory(Path.Combine(mainPath, "WebApp"));
        Directory.CreateDirectory(Path.Combine(cursorPath, "ComplaincePlatform"));
        Directory.CreateDirectory(Path.Combine(claudePath, "ComplaincePlatform"));

        workspaceStore = Path.Combine(root, "development-workspaces.json");
        File.WriteAllText(workspaceStore, JsonSerializer.Serialize(
            DevelopmentWorkspaceRegistry.BuildDefaults(root),
            new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter() }
            }));

        git = new FakeGitWorkspaceService();
        git.SetState(mainPath, "main", "main-head-sha", accessible: true);
        git.SetState(cursorPath, "agent/cursor/active", "cursor-head-sha", accessible: true);
        git.SetState(claudePath, "agent/claude/active", "claude-head-sha", accessible: true);

        workspaceRegistry = new DevelopmentWorkspaceRegistry(workspaceStore);
        sourceContexts = new SourceContextService(workspaceRegistry, git);
        folderRegistry = new SolutionFolderRegistry(Path.Combine(root, "solution-folders.json"));
        discoveryBoundary = new SourceDiscoveryBoundary(folderRegistry);
        discoveryService = new ServiceDiscoveryService(sourceContexts, discoveryBoundary, new NoopLoggingService());
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
    public async Task SharedSourceContext_ResolvesAoviasMain()
    {
        var shared = await sourceContexts.ResolveAsync(SourceContextIds.Shared);
        Assert.Equal(SourceContextKind.Shared, shared.Kind);
        Assert.Equal(mainPath, shared.RootPath, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("main", shared.Branch);
        Assert.Equal("main-head-sha", shared.Head);
        Assert.Equal("AOVIAS / Shared", shared.Ownership);
    }

    [Fact]
    public async Task CursorSourceContext_ResolvesAoviasCursor()
    {
        var cursor = await sourceContexts.ResolveAsync(SourceContextIds.Cursor);
        Assert.Equal(SourceContextKind.Workspace, cursor.Kind);
        Assert.Equal(cursorPath, cursor.RootPath, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(SourceContextIds.Cursor, cursor.WorkspaceId);
        Assert.Equal("Cursor", cursor.Ownership);
    }

    [Fact]
    public async Task ClaudeSourceContext_ResolvesAoviasClaude()
    {
        var claude = await sourceContexts.ResolveAsync(SourceContextIds.Claude);
        Assert.Equal(SourceContextKind.Workspace, claude.Kind);
        Assert.Equal(claudePath, claude.RootPath, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(SourceContextIds.Claude, claude.WorkspaceId);
        Assert.Equal("Claude", claude.Ownership);
    }

    [Fact]
    public async Task CursorBranchValidation_UsesAgentCursorPrefix()
    {
        var cursor = await sourceContexts.ResolveAsync(SourceContextIds.Cursor);
        Assert.StartsWith("agent/cursor/", cursor.Branch);
        Assert.True(SourceContextValidator.BranchMatchesExpected(cursor, cursor.Branch));

        git.SetState(cursorPath, "main", "bad-head", accessible: true);
        cursor = await sourceContexts.ResolveAsync(SourceContextIds.Cursor);
        Assert.Equal(SourceContextGitValidity.InvalidBranch, cursor.GitValidity);
    }

    [Fact]
    public async Task ClaudeBranchValidation_UsesAgentClaudePrefix()
    {
        var claude = await sourceContexts.ResolveAsync(SourceContextIds.Claude);
        Assert.StartsWith("agent/claude/", claude.Branch);
        Assert.True(SourceContextValidator.BranchMatchesExpected(claude, claude.Branch));

        git.SetState(claudePath, "main", "bad-head", accessible: true);
        claude = await sourceContexts.ResolveAsync(SourceContextIds.Claude);
        Assert.Equal(SourceContextGitValidity.InvalidBranch, claude.GitValidity);
    }

    [Fact]
    public async Task SharedContext_DoesNotAcceptAgentWorktreeSource()
    {
        var shared = await sourceContexts.ResolveAsync(SourceContextIds.Shared);
        var cursorProject = Path.Combine(cursorPath, "ComplaincePlatform", "Gateway", "Gateway.csproj");
        Directory.CreateDirectory(Path.GetDirectoryName(cursorProject)!);
        await File.WriteAllTextAsync(cursorProject, MinimalWebCsproj("Gateway"));

        Assert.False(sourceContexts.AcceptsSourcePath(shared, cursorProject));
    }

    [Fact]
    public async Task CursorContext_CannotResolveMainAsSource()
    {
        var cursor = await sourceContexts.ResolveAsync(SourceContextIds.Cursor);
        var mainProject = Path.Combine(mainPath, "ComplaincePlatform", "Gateway", "Gateway.csproj");
        Directory.CreateDirectory(Path.GetDirectoryName(mainProject)!);
        await File.WriteAllTextAsync(mainProject, MinimalWebCsproj("Gateway"));

        Assert.False(sourceContexts.AcceptsSourcePath(cursor, mainProject));
    }

    [Fact]
    public async Task ClaudeContext_CannotResolveMainAsSource()
    {
        var claude = await sourceContexts.ResolveAsync(SourceContextIds.Claude);
        var mainProject = Path.Combine(mainPath, "ComplaincePlatform", "Gateway", "Gateway.csproj");
        Assert.False(sourceContexts.AcceptsSourcePath(claude, mainProject));
    }

    [Fact]
    public async Task ConfiguringCursorDiscovery_DoesNotAlterSharedSourceContext()
    {
        var sharedBefore = await sourceContexts.ResolveAsync(SourceContextIds.Shared);
        folderRegistry.Add(Path.Combine(cursorPath, "ComplaincePlatform"));
        folderRegistry.Add(Path.Combine(cursorPath, "WebApp"));

        var sharedAfter = await sourceContexts.ResolveAsync(SourceContextIds.Shared);
        Assert.Equal(sharedBefore.RootPath, sharedAfter.RootPath, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(sharedBefore.Branch, sharedAfter.Branch);
        Assert.Equal(sharedBefore.Head, sharedAfter.Head);

        var sharedRoots = discoveryBoundary.GetDiscoveryRoots(sharedAfter);
        Assert.All(sharedRoots, root => Assert.StartsWith(mainPath, root, StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(sharedRoots, root => root.Contains("AOVIAS.Cursor", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ConfiguringClaudeDiscovery_DoesNotAlterSharedSourceContext()
    {
        var sharedBefore = await sourceContexts.ResolveAsync(SourceContextIds.Shared);
        folderRegistry.Add(Path.Combine(claudePath, "ComplaincePlatform"));

        var sharedAfter = await sourceContexts.ResolveAsync(SourceContextIds.Shared);
        Assert.Equal(sharedBefore.RootPath, sharedAfter.RootPath, StringComparer.OrdinalIgnoreCase);

        var sharedRoots = discoveryBoundary.GetDiscoveryRoots(sharedAfter);
        Assert.DoesNotContain(sharedRoots, root => root.Contains("AOVIAS.Claude", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task WorkspaceSourceFailure_AffectsOnlyThatWorkspace()
    {
        git.SetState(cursorPath, state: null, accessible: false, error: "Cursor unavailable");

        var all = await sourceContexts.ResolveAllAsync();
        var shared = all.Single(item => item.ContextId == SourceContextIds.Shared);
        var cursor = all.Single(item => item.ContextId == SourceContextIds.Cursor);
        var claude = all.Single(item => item.ContextId == SourceContextIds.Claude);

        Assert.Equal(SourceContextGitValidity.Accessible, shared.GitValidity);
        Assert.Equal(SourceContextGitValidity.Unavailable, cursor.GitValidity);
        Assert.Equal(SourceContextGitValidity.Accessible, claude.GitValidity);
    }

    [Fact]
    public async Task SourceContextResolution_DoesNotMutateGit()
    {
        git.ResetMutationLog();
        _ = await sourceContexts.ResolveAllAsync();
        Assert.Empty(git.MutationCommands);
        Assert.All(git.ExecutedCommands, cmd => Assert.True(IsReadOnlyGitCommand(cmd), $"Unexpected git mutation: {cmd}"));
    }

    [Fact]
    public async Task SharedDiscovery_UsesOnlySharedRoots()
    {
        folderRegistry.Add(Path.Combine(cursorPath, "ComplaincePlatform"));
        var shared = await sourceContexts.ResolveAsync(SourceContextIds.Shared);
        var discovered = await discoveryService.DiscoverAsync(shared, CancellationToken.None);
        Assert.All(discovered, service =>
            Assert.StartsWith(mainPath, service.ProjectPath, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CursorDiscovery_UsesWorkspaceRootsOnly()
    {
        folderRegistry.Add(Path.Combine(cursorPath, "ComplaincePlatform"));
        var cursor = await sourceContexts.ResolveAsync(SourceContextIds.Cursor);
        var roots = discoveryBoundary.GetDiscoveryRoots(cursor);
        Assert.All(roots, root => Assert.StartsWith(cursorPath, root, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsReadOnlyGitCommand(string command)
    {
        var readOnlyPrefixes = new[]
        {
            "rev-parse",
            "status --porcelain",
            "worktree list",
            "rev-list --left-right --count"
        };

        return readOnlyPrefixes.Any(prefix => command.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private static string MinimalWebCsproj(string name)
        => $"""
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>
              <ItemGroup><PackageReference Include="Microsoft.AspNetCore.App" /></ItemGroup>
            </Project>
            """;

    private sealed class FakeGitWorkspaceService : IGitWorkspaceService
    {
        private readonly Dictionary<string, GitWorkspaceState> states = new(StringComparer.OrdinalIgnoreCase);
        public IReadOnlyList<string> ExecutedCommands { get; private set; } = [];
        public IReadOnlyList<string> MutationCommands { get; private set; } = [];
        private readonly List<string> executed = [];
        private readonly List<string> mutations = [];

        public void SetState(
            string path,
            string? branch,
            string? head = null,
            bool accessible = true,
            string? error = null)
        {
            states[Path.GetFullPath(path)] = accessible
                ? new GitWorkspaceState
                {
                    Path = Path.GetFullPath(path),
                    IsAccessible = true,
                    Branch = branch,
                    Head = head ?? "deadbeef",
                    IsDetached = string.IsNullOrWhiteSpace(branch),
                    IsClean = true,
                    ObservedAt = DateTimeOffset.UtcNow
                }
                : new GitWorkspaceState
                {
                    Path = Path.GetFullPath(path),
                    IsAccessible = false,
                    Error = error ?? "Unavailable",
                    ObservedAt = DateTimeOffset.UtcNow
                };
        }

        public void SetState(string path, GitWorkspaceState? state, bool accessible, string? error = null)
        {
            if (!accessible || state is null)
            {
                SetState(path, branch: null, accessible: false, error: error);
                return;
            }

            SetState(path, state.Branch, state.Head, accessible: true);
        }

        public void ResetMutationLog()
        {
            executed.Clear();
            mutations.Clear();
            ExecutedCommands = executed;
            MutationCommands = mutations;
        }

        public bool LooksLikeGitWorktree(string path)
            => states.ContainsKey(Path.GetFullPath(path));

        public Task<IReadOnlyList<GitWorktreeInfo>> ListWorktreesAsync(string repositoryOrWorktreePath, CancellationToken cancellationToken = default)
        {
            Record("worktree list --porcelain");
            return Task.FromResult<IReadOnlyList<GitWorktreeInfo>>(Array.Empty<GitWorktreeInfo>());
        }

        public Task<GitWorkspaceState> GetWorkspaceStateAsync(string worktreePath, CancellationToken cancellationToken = default)
        {
            Record("rev-parse --abbrev-ref HEAD");
            Record("rev-parse HEAD");
            Record("status --porcelain");
            Record("rev-list --left-right --count @{upstream}...HEAD");
            var full = Path.GetFullPath(worktreePath);
            return Task.FromResult(states.TryGetValue(full, out var state)
                ? state
                : new GitWorkspaceState
                {
                    Path = full,
                    IsAccessible = false,
                    Error = "Missing",
                    ObservedAt = DateTimeOffset.UtcNow
                });
        }

        private void Record(string command)
        {
            executed.Add(command);
            if (!IsReadOnlyGitCommand(command))
            {
                mutations.Add(command);
            }
        }
    }

    private sealed class NoopLoggingService : ILoggingService
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
