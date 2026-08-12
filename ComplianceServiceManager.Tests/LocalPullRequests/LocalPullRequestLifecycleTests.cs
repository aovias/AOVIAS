using System.Diagnostics;
using System.IO;
using ComplianceServiceManager.Logging;
using ComplianceServiceManager.Models;
using ComplianceServiceManager.Models.LocalPullRequests;
using ComplianceServiceManager.Models.Workspaces;
using ComplianceServiceManager.Services.LocalPullRequests;
using ComplianceServiceManager.Services.Workspaces;
using System.Collections.ObjectModel;

namespace ComplianceServiceManager.Tests.LocalPullRequests;

public sealed class LocalPullRequestLifecycleTests : IDisposable
{
    private readonly string root;
    private readonly string mainWorktree;
    private readonly string agentWorktree;
    private readonly string storePath;
    private readonly LocalPullRequestService service;

    public LocalPullRequestLifecycleTests()
    {
        root = Path.Combine(Path.GetTempPath(), "aovias-local-pr-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        mainWorktree = Path.Combine(root, "AOVIAS");
        agentWorktree = Path.Combine(root, "AOVIAS.Cursor");
        Directory.CreateDirectory(mainWorktree);

        RunGit(mainWorktree, "init");
        RunGit(mainWorktree, "config user.email test@example.com");
        RunGit(mainWorktree, "config user.name Test");
        File.WriteAllText(Path.Combine(mainWorktree, "README.md"), "main\n");
        RunGit(mainWorktree, "add README.md");
        RunGit(mainWorktree, "commit -m init");
        RunGit(mainWorktree, "branch -M main");
        RunGit(mainWorktree, $"worktree add \"{agentWorktree}\" -b agent/cursor/feature-test");

        File.WriteAllText(Path.Combine(agentWorktree, "feature.txt"), "hello\n");
        RunGit(agentWorktree, "add feature.txt");
        RunGit(agentWorktree, "commit -m feature");

        storePath = Path.Combine(root, "local-pull-requests.json");
        var workspaceStore = Path.Combine(root, "development-workspaces.json");
        File.WriteAllText(workspaceStore, System.Text.Json.JsonSerializer.Serialize(
            DevelopmentWorkspaceRegistry.BuildDefaults(root),
            new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            }));

        var registry = new DevelopmentWorkspaceRegistry(workspaceStore);
        var gitRunner = new GitCliRunner(() => new[] { root, mainWorktree, agentWorktree });
        var gitPr = new GitPullRequestService(gitRunner);
        var workspaceGit = new GitWorkspaceService(gitRunner);
        service = new LocalPullRequestService(
            new LocalPullRequestRegistry(storePath),
            gitPr,
            workspaceGit,
            registry,
            new NoopLogger());
    }

    public void Dispose()
    {
        try
        {
            // Detach worktree then delete.
            try
            {
                RunGit(mainWorktree, $"worktree remove --force \"{agentWorktree}\"");
            }
            catch
            {
            }

            Directory.Delete(root, recursive: true);
        }
        catch
        {
        }
    }

    [Fact]
    public async Task Create_Refresh_ShowsReadyForReview()
    {
        var pr = await service.CreateAsync(
            "Feature",
            "desc",
            "agent/cursor/feature-test",
            "main",
            agentWorktree,
            mainWorktree,
            "Cursor");

        Assert.Equal(LocalPullRequestStatus.ReadyForReview, pr.Status);
        Assert.True(pr.Comparison!.Ahead >= 1);
        Assert.Contains(pr.ChangedFiles, file => file.Path.Contains("feature.txt", StringComparison.OrdinalIgnoreCase));
        Assert.NotEmpty(pr.Commits);
    }

    [Fact]
    public async Task DirtySource_BlocksReview()
    {
        File.WriteAllText(Path.Combine(agentWorktree, "dirty.txt"), "x");
        var pr = await service.CreateAsync(
            "Dirty",
            "",
            "agent/cursor/feature-test",
            "main",
            agentWorktree,
            mainWorktree,
            "Cursor");

        Assert.Equal(LocalPullRequestStatus.Blocked, pr.Status);
        Assert.Contains("uncommitted", pr.BlockReason ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SourceEqualsTarget_Rejected()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(
            "Same",
            "",
            "main",
            "main",
            mainWorktree,
            mainWorktree,
            "Cursor"));
    }

    [Fact]
    public async Task SourceMain_Rejected()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(
            "FromMain",
            "",
            "main",
            "agent/cursor/feature-test",
            mainWorktree,
            agentWorktree,
            "Cursor"));
    }

    [Fact]
    public async Task Approve_TiedToExactShas_AndMergeSucceeds()
    {
        var pr = await service.CreateAsync(
            "MergeMe",
            "",
            "agent/cursor/feature-test",
            "main",
            agentWorktree,
            mainWorktree,
            "Cursor");

        await service.ApproveAsync(pr, "Human", "LGTM");
        Assert.Equal(LocalPullRequestStatus.Approved, pr.Status);
        Assert.False(string.IsNullOrWhiteSpace(pr.LastReview?.SourceCommitSha));
        Assert.False(string.IsNullOrWhiteSpace(pr.LastReview?.TargetCommitSha));

        var (ok, error) = await service.MergeAsync(pr);
        Assert.True(ok, error);
        Assert.Equal(LocalPullRequestStatus.Merged, pr.Status);
        Assert.False(string.IsNullOrWhiteSpace(pr.MergeCommitSha));
        Assert.True(File.Exists(Path.Combine(mainWorktree, "feature.txt")));
    }

    [Fact]
    public async Task ApprovalBecomesStale_AfterSourceChanges()
    {
        var pr = await service.CreateAsync(
            "Stale",
            "",
            "agent/cursor/feature-test",
            "main",
            agentWorktree,
            mainWorktree,
            "Cursor");
        await service.ApproveAsync(pr, "Human", null);

        File.WriteAllText(Path.Combine(agentWorktree, "more.txt"), "more\n");
        RunGit(agentWorktree, "add more.txt");
        RunGit(agentWorktree, "commit -m more");

        await service.RefreshAsync(pr);
        Assert.Equal(LocalPullRequestStatus.ReadyForReview, pr.Status);
        Assert.True(pr.Comparison!.ApprovalIsStale);

        var (ok, error) = await service.MergeAsync(pr);
        Assert.False(ok);
        Assert.Contains("APPROVED", error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RequestChanges_ThenNewCommit_ReturnsReady()
    {
        var pr = await service.CreateAsync(
            "Changes",
            "",
            "agent/cursor/feature-test",
            "main",
            agentWorktree,
            mainWorktree,
            "Cursor");
        await service.RequestChangesAsync(pr, "Human", "Please fix");
        Assert.Equal(LocalPullRequestStatus.ChangesRequested, pr.Status);

        File.WriteAllText(Path.Combine(agentWorktree, "fix.txt"), "fix\n");
        RunGit(agentWorktree, "add fix.txt");
        RunGit(agentWorktree, "commit -m fix");
        await service.RefreshAsync(pr);
        Assert.Equal(LocalPullRequestStatus.ReadyForReview, pr.Status);
    }

    [Fact]
    public async Task Merge_WithoutApproval_Blocked()
    {
        var pr = await service.CreateAsync(
            "NoApprove",
            "",
            "agent/cursor/feature-test",
            "main",
            agentWorktree,
            mainWorktree,
            "Cursor");
        var (ok, error) = await service.MergeAsync(pr);
        Assert.False(ok);
        Assert.Contains("APPROVED", error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DirtyTarget_BlocksMerge()
    {
        var pr = await service.CreateAsync(
            "DirtyTarget",
            "",
            "agent/cursor/feature-test",
            "main",
            agentWorktree,
            mainWorktree,
            "Cursor");
        await service.ApproveAsync(pr, "Human", null);

        File.WriteAllText(Path.Combine(mainWorktree, "main-dirty.txt"), "x");
        var (ok, error) = await service.MergeAsync(pr);
        Assert.False(ok);
        Assert.Contains("dirty", error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Merge_Blocked_WhenTargetWorktreeNotOnTargetBranch()
    {
        var pr = await service.CreateAsync(
            "WrongCheckout",
            "",
            "agent/cursor/feature-test",
            "main",
            agentWorktree,
            mainWorktree,
            "Cursor");
        await service.ApproveAsync(pr, "Human", null);

        RunGit(mainWorktree, "checkout -b agent/cursor/not-main");
        try
        {
            var (ok, error) = await service.MergeAsync(pr);
            Assert.False(ok);
            Assert.Contains("target branch", error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            Assert.False(File.Exists(Path.Combine(mainWorktree, "feature.txt")));
        }
        finally
        {
            RunGit(mainWorktree, "checkout main");
        }
    }

    [Fact]
    public async Task Persistence_RoundTrip()
    {
        var pr = await service.CreateAsync(
            "Persist",
            "body",
            "agent/cursor/feature-test",
            "main",
            agentWorktree,
            mainWorktree,
            "Cursor");

        var reloaded = new LocalPullRequestRegistry(storePath).GetAll();
        Assert.Contains(reloaded, item => item.Id == pr.Id && item.Title == "Persist");
    }

    [Fact]
    public async Task DiffAndCommits_Readable()
    {
        var pr = await service.CreateAsync(
            "Diff",
            "",
            "agent/cursor/feature-test",
            "main",
            agentWorktree,
            mainWorktree,
            "Cursor");
        var path = pr.ChangedFiles.First().Path;
        var diff = await service.LoadFileDiffAsync(pr, path);
        Assert.False(string.IsNullOrWhiteSpace(diff));
        Assert.Contains("feature", diff, StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(pr.Commits);
    }

    [Fact]
    public void Registry_EmptyStore_StartsEmpty()
    {
        var emptyPath = Path.Combine(root, "empty-prs.json");
        var registry = new LocalPullRequestRegistry(emptyPath);
        Assert.Empty(registry.GetAll());
    }

    private static void RunGit(string cwd, string args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = args,
            WorkingDirectory = cwd,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var process = Process.Start(psi) ?? throw new InvalidOperationException("git failed to start");
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"git {args} failed: {stderr}{stdout}");
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

public sealed class GitPullRequestServiceConflictTests
{
    [Fact]
    public async Task DetectConflicts_WhenBothSidesEditSameFile()
    {
        var root = Path.Combine(Path.GetTempPath(), "aovias-conflict-" + Guid.NewGuid().ToString("N"));
        var main = Path.Combine(root, "main");
        Directory.CreateDirectory(main);
        try
        {
            Run(main, "init");
            Run(main, "config user.email t@e.com");
            Run(main, "config user.name t");
            File.WriteAllText(Path.Combine(main, "shared.txt"), "base\n");
            Run(main, "add shared.txt");
            Run(main, "commit -m base");
            Run(main, "branch -M main");
            Run(main, "checkout -b feature");
            File.WriteAllText(Path.Combine(main, "shared.txt"), "feature\n");
            Run(main, "add shared.txt");
            Run(main, "commit -m feature");
            Run(main, "checkout main");
            File.WriteAllText(Path.Combine(main, "shared.txt"), "main\n");
            Run(main, "add shared.txt");
            Run(main, "commit -m mainchange");

            var git = new GitPullRequestService(new GitCliRunner(() => new[] { root, main }));
            var (hasConflicts, summary) = await git.DetectMergeConflictsAsync(main, "feature", "main");
            Assert.True(hasConflicts);
            Assert.False(string.IsNullOrWhiteSpace(summary));
        }
        finally
        {
            try
            {
                Directory.Delete(root, true);
            }
            catch
            {
            }
        }
    }

    private static void Run(string cwd, string args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = args,
            WorkingDirectory = cwd,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var process = Process.Start(psi)!;
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(process.StandardError.ReadToEnd());
        }
    }
}
