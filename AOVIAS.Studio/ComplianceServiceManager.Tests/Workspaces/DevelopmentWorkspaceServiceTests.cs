using ComplianceServiceManager.Logging;
using ComplianceServiceManager.Models;
using ComplianceServiceManager.Models.Workspaces;
using ComplianceServiceManager.Services.Workspaces;

namespace ComplianceServiceManager.Tests.Workspaces;

public sealed class DevelopmentWorkspaceServiceTests
{
    [Fact]
    public async Task RefreshAsync_UsesConfiguredOwnershipAndGitState()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "aovias-dev-ws-svc-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        var storePath = Path.Combine(tempRoot, "development-workspaces.json");

        try
        {
            // Seed registry with a synthetic path under tempRoot defaults by writing after construction.
            var registry = new DevelopmentWorkspaceRegistry(storePath);
            var git = new FakeGitWorkspaceService();
            var logger = new NoopLoggingService();
            var service = new DevelopmentWorkspaceService(registry, git, logger);

            var items = await service.RefreshAsync();
            Assert.Equal(3, items.Count);

            var cursor = items.Single(item => item.Id == "cursor");
            Assert.Equal(WorkspaceAgentOwner.Cursor, cursor.AgentOwner);
            Assert.Equal("Cursor", cursor.AgentOwnerDisplay);
            Assert.Equal(WorkspaceAvailability.Unavailable, cursor.Availability);
        }
        finally
        {
            try
            {
                Directory.Delete(tempRoot, recursive: true);
            }
            catch
            {
            }
        }
    }

    private sealed class FakeGitWorkspaceService : IGitWorkspaceService
    {
        public bool LooksLikeGitWorktree(string path) => false;

        public Task<IReadOnlyList<GitWorktreeInfo>> ListWorktreesAsync(string repositoryOrWorktreePath, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<GitWorktreeInfo>>([]);

        public Task<GitWorkspaceState> GetWorkspaceStateAsync(string worktreePath, CancellationToken cancellationToken = default)
            => Task.FromResult(new GitWorkspaceState
            {
                Path = worktreePath,
                IsAccessible = false,
                Error = "Git repository cannot be accessed.",
                ObservedAt = DateTimeOffset.UtcNow
            });
    }

    private sealed class NoopLoggingService : ILoggingService
    {
        public System.Collections.ObjectModel.ObservableCollection<LogEntryModel> Entries { get; } = [];

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
