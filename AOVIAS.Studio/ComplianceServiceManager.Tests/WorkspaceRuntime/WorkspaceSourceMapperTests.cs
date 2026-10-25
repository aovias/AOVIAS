using ComplianceServiceManager.Services.WorkspaceRuntime;

namespace ComplianceServiceManager.Tests.WorkspaceRuntime;

public sealed class WorkspaceSourceMapperTests
{
    [Fact]
    public void Resolve_MapsMainPathOntoCursorWorktree()
    {
        var root = Path.Combine(Path.GetTempPath(), "aovias-map-" + Guid.NewGuid().ToString("N"));
        var main = Path.Combine(root, "AOVIAS");
        var cursor = Path.Combine(root, "AOVIAS.Cursor");
        Directory.CreateDirectory(Path.Combine(main, "OrganizationService"));
        Directory.CreateDirectory(Path.Combine(cursor, "OrganizationService"));
        try
        {
            var mapped = WorkspaceSourceMapper.ResolveUnderWorkspace(
                Path.Combine(main, "OrganizationService"),
                cursor,
                [main, cursor]);
            Assert.Equal(Path.Combine(cursor, "OrganizationService"), mapped);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ExpectedBranchPrefix_IsAgentScoped()
    {
        Assert.Equal("agent/cursor/", WorkspaceSourceMapper.ExpectedBranchPrefix("cursor"));
        Assert.Equal("agent/claude/", WorkspaceSourceMapper.ExpectedBranchPrefix("claude"));
        Assert.Equal(string.Empty, WorkspaceSourceMapper.ExpectedBranchPrefix("main"));
    }
}
