using ComplianceServiceManager.Models.Workspaces;
using ComplianceServiceManager.Services.Workspaces;

namespace ComplianceServiceManager.Tests.Workspaces;

public sealed class DevelopmentWorkspaceRegistryTests
{
    [Fact]
    public void BuildDefaults_MapsAoviasTopologyAndAgentOwnership()
    {
        var document = DevelopmentWorkspaceRegistry.BuildDefaults(@"V:\DevelopmentRepo");

        var repository = Assert.Single(document.Repositories);
        Assert.Equal("aovias", repository.Id);
        Assert.Equal(@"V:\DevelopmentRepo\AOVIAS", repository.RootPath);
        Assert.Equal(3, repository.Workspaces.Count);

        var main = repository.Workspaces.Single(workspace => workspace.Id == "main");
        Assert.Equal(WorkspaceAgentOwner.Integration, main.AgentOwner);
        Assert.Equal(WorkspaceRole.Integration, main.Role);
        Assert.True(main.IsMain);
        Assert.Equal(@"V:\DevelopmentRepo\AOVIAS", main.Path);

        var cursor = repository.Workspaces.Single(workspace => workspace.Id == "cursor");
        Assert.Equal(WorkspaceAgentOwner.Cursor, cursor.AgentOwner);
        Assert.Equal(@"V:\DevelopmentRepo\AOVIAS.Cursor", cursor.Path);

        var claude = repository.Workspaces.Single(workspace => workspace.Id == "claude");
        Assert.Equal(WorkspaceAgentOwner.Claude, claude.AgentOwner);
        Assert.Equal(@"V:\DevelopmentRepo\AOVIAS.Claude", claude.Path);
    }

    [Fact]
    public void Registry_PersistsConfigurationAndExposesAllowedRoots()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "aovias-workspace-registry-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        var storePath = Path.Combine(tempRoot, "development-workspaces.json");

        try
        {
            var registry = new DevelopmentWorkspaceRegistry(storePath);
            var roots = registry.GetAllowedRoots();

            Assert.Contains(roots, root => root.EndsWith("AOVIAS", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(roots, root => root.EndsWith("AOVIAS.Cursor", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(roots, root => root.EndsWith("AOVIAS.Claude", StringComparison.OrdinalIgnoreCase));
            Assert.True(File.Exists(storePath));
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
}
