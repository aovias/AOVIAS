using System.Diagnostics;
using ComplianceServiceManager.Models.Workspaces;
using ComplianceServiceManager.Services.Workspaces;

namespace ComplianceServiceManager.Tests.Workspaces;

public sealed class GitWorkspaceServiceFixtureTests : IAsyncLifetime
{
    private string fixtureRoot = string.Empty;
    private string mainRepo = string.Empty;
    private string linkedWorktree = string.Empty;
    private GitWorkspaceService service = null!;
    private readonly List<string> allowedRoots = [];

    public async Task InitializeAsync()
    {
        fixtureRoot = Path.Combine(Path.GetTempPath(), "aovias-git-ws-" + Guid.NewGuid().ToString("N"));
        mainRepo = Path.Combine(fixtureRoot, "main");
        linkedWorktree = Path.Combine(fixtureRoot, "linked");
        Directory.CreateDirectory(mainRepo);

        await RunGitAsync(mainRepo, "init");
        await RunGitAsync(mainRepo, "config user.email aovias-test@example.com");
        await RunGitAsync(mainRepo, "config user.name Aovias Test");
        await RunGitAsync(mainRepo, "checkout -b main");
        await RunGitAsync(mainRepo, "commit --allow-empty -m init");
        await RunGitAsync(mainRepo, $"worktree add \"{linkedWorktree}\" -b agent/cursor/fixture");

        allowedRoots.Add(Path.GetFullPath(mainRepo));
        allowedRoots.Add(Path.GetFullPath(linkedWorktree));
        allowedRoots.Add(Path.GetFullPath(fixtureRoot));

        var runner = new GitCliRunner(() => allowedRoots);
        service = new GitWorkspaceService(runner);
    }

    public Task DisposeAsync()
    {
        try
        {
            if (Directory.Exists(linkedWorktree))
            {
                RunGitAsync(mainRepo, $"worktree remove \"{linkedWorktree}\" --force").GetAwaiter().GetResult();
            }
        }
        catch
        {
        }

        try
        {
            if (Directory.Exists(fixtureRoot))
            {
                Directory.Delete(fixtureRoot, recursive: true);
            }
        }
        catch
        {
            // Best-effort cleanup on Windows file locks.
        }

        return Task.CompletedTask;
    }

    [Fact]
    public void LooksLikeGitWorktree_DetectsGitDirectoryAndGitFile()
    {
        Assert.True(service.LooksLikeGitWorktree(mainRepo));
        Assert.True(Directory.Exists(Path.Combine(mainRepo, ".git")) || File.Exists(Path.Combine(mainRepo, ".git")));

        Assert.True(service.LooksLikeGitWorktree(linkedWorktree));
        Assert.True(File.Exists(Path.Combine(linkedWorktree, ".git")));
        Assert.False(Directory.Exists(Path.Combine(linkedWorktree, ".git")));
    }

    [Fact]
    public async Task ListWorktreesAsync_DiscoversMainAndLinked()
    {
        var worktrees = await service.ListWorktreesAsync(mainRepo);
        Assert.Contains(worktrees, item => PathsEqual(item.Path, mainRepo));
        Assert.Contains(worktrees, item => PathsEqual(item.Path, linkedWorktree));
    }

    [Fact]
    public async Task GetWorkspaceStateAsync_ReadsBranchHeadAndClean()
    {
        var state = await service.GetWorkspaceStateAsync(mainRepo);
        Assert.True(state.IsAccessible);
        Assert.Equal("main", state.Branch);
        Assert.False(string.IsNullOrWhiteSpace(state.Head));
        Assert.True(state.IsClean);
        Assert.False(state.IsDetached);
    }

    [Fact]
    public async Task GetWorkspaceStateAsync_DetectsDirtyWorkingTree()
    {
        var dirtyFile = Path.Combine(mainRepo, "dirty.txt");
        await File.WriteAllTextAsync(dirtyFile, "dirty");

        try
        {
            var state = await service.GetWorkspaceStateAsync(mainRepo);
            Assert.True(state.IsAccessible);
            Assert.False(state.IsClean);
        }
        finally
        {
            File.Delete(dirtyFile);
        }
    }

    [Fact]
    public async Task GetWorkspaceStateAsync_LinkedWorktreeBranch()
    {
        var state = await service.GetWorkspaceStateAsync(linkedWorktree);
        Assert.True(state.IsAccessible);
        Assert.Equal("agent/cursor/fixture", state.Branch);
        Assert.True(state.IsClean);
    }

    [Fact]
    public async Task GetWorkspaceStateAsync_MissingPath_IsUnavailable()
    {
        var missing = Path.Combine(fixtureRoot, "does-not-exist");
        var state = await service.GetWorkspaceStateAsync(missing);
        Assert.False(state.IsAccessible);
        Assert.False(state.IsClean);
        Assert.Contains("missing", state.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetWorkspaceStateAsync_InvalidWorktree_IsUnavailable()
    {
        var invalid = Path.Combine(fixtureRoot, "not-a-repo");
        Directory.CreateDirectory(invalid);
        allowedRoots.Add(Path.GetFullPath(invalid));

        var state = await service.GetWorkspaceStateAsync(invalid);
        Assert.False(state.IsAccessible);
        Assert.Contains("not a Git worktree", state.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DevelopmentWorkspaceItem_AppliesOwnershipAndUnavailableState()
    {
        var item = new DevelopmentWorkspaceItem
        {
            Id = "cursor",
            DisplayName = "CURSOR",
            Path = linkedWorktree,
            AgentOwner = WorkspaceAgentOwner.Cursor,
            Role = WorkspaceRole.Agent
        };
        item.ApplyConfigurationDisplays();
        Assert.Equal("Cursor", item.AgentOwnerDisplay);

        item.ApplyGitState(new GitWorkspaceState
        {
            Path = linkedWorktree,
            IsAccessible = false,
            Error = "Git repository cannot be accessed.",
            ObservedAt = DateTimeOffset.UtcNow
        });

        Assert.Equal(WorkspaceAvailability.Unavailable, item.Availability);
        Assert.Equal("UNAVAILABLE", item.StatusDisplay);
        Assert.Equal("Git repository cannot be accessed.", item.StatusReason);
    }

    private static bool PathsEqual(string left, string right)
        => string.Equals(Path.GetFullPath(left).TrimEnd('\\', '/'), Path.GetFullPath(right).TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase);

    private static async Task RunGitAsync(string workingDirectory, string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Unable to start git.");
        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"git {arguments} failed: {stderr}\n{stdout}");
        }
    }
}
