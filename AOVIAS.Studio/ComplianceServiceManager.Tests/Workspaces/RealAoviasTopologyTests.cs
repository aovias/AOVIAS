using ComplianceServiceManager.Logging;
using ComplianceServiceManager.Models;
using ComplianceServiceManager.Models.Workspaces;
using ComplianceServiceManager.Services.Workspaces;
using System.Collections.ObjectModel;

namespace ComplianceServiceManager.Tests.Workspaces;

public sealed class RealAoviasTopologyTests
{
    [Fact]
    public async Task RefreshAsync_MatchesKnownLocalAoviasTopology_WhenPresent()
    {
        var main = @"V:\DevelopmentRepo\AOVIAS";
        var cursor = @"V:\DevelopmentRepo\AOVIAS.Cursor";
        var claude = @"V:\DevelopmentRepo\AOVIAS.Claude";
        if (!Directory.Exists(main) || !Directory.Exists(cursor) || !Directory.Exists(claude))
        {
            return; // Skip gracefully on machines without the AOVIAS topology.
        }

        var storePath = Path.Combine(Path.GetTempPath(), "aovias-real-topology-" + Guid.NewGuid().ToString("N"), "development-workspaces.json");
        Directory.CreateDirectory(Path.GetDirectoryName(storePath)!);

        try
        {
            // Force defaults rooted at V:\DevelopmentRepo regardless of Studio BaseDirectory.
            File.WriteAllText(storePath, System.Text.Json.JsonSerializer.Serialize(
                DevelopmentWorkspaceRegistry.BuildDefaults(@"V:\DevelopmentRepo"),
                new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                }));

            var registry = new DevelopmentWorkspaceRegistry(storePath);
            var git = new GitWorkspaceService(new GitCliRunner(() => registry.GetAllowedRoots()));
            var service = new DevelopmentWorkspaceService(registry, git, new NoopLoggingService());
            var items = await service.RefreshAsync();

            Assert.Equal(3, items.Count);

            var mainItem = items.Single(item => item.Id == "main");
            Assert.Equal("main", mainItem.Branch);
            Assert.Equal(WorkspaceAvailability.Clean, mainItem.Availability);
            Assert.Equal(WorkspaceAgentOwner.Integration, mainItem.AgentOwner);
            Assert.True(mainItem.IsMain);

            var cursorItem = items.Single(item => item.Id == "cursor");
            Assert.StartsWith("agent/cursor/", cursorItem.Branch);
            Assert.Equal(WorkspaceAgentOwner.Cursor, cursorItem.AgentOwner);
            Assert.True(File.Exists(Path.Combine(cursor, ".git")));
            Assert.False(Directory.Exists(Path.Combine(cursor, ".git")));
            // Cursor worktree is expected dirty while D1 is under active development.
            Assert.True(
                cursorItem.Availability is WorkspaceAvailability.Clean or WorkspaceAvailability.Dirty,
                $"Unexpected cursor availability: {cursorItem.Availability}");

            var claudeItem = items.Single(item => item.Id == "claude");
            Assert.Equal("agent/claude/active", claudeItem.Branch);
            Assert.True(
                claudeItem.Availability is WorkspaceAvailability.Clean or WorkspaceAvailability.Dirty,
                $"Unexpected claude availability: {claudeItem.Availability}");
            Assert.Equal(WorkspaceAgentOwner.Claude, claudeItem.AgentOwner);
            Assert.True(File.Exists(Path.Combine(claude, ".git")));
        }
        finally
        {
            try
            {
                Directory.Delete(Path.GetDirectoryName(storePath)!, recursive: true);
            }
            catch
            {
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
