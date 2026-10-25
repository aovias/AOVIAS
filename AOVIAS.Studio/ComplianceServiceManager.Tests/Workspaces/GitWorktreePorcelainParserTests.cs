using ComplianceServiceManager.Services.Workspaces;

namespace ComplianceServiceManager.Tests.Workspaces;

public sealed class GitWorktreePorcelainParserTests
{
    [Fact]
    public void ParsePorcelainWorktrees_ReadsMainAndLinkedWorktrees()
    {
        const string output =
            """
            worktree V:/DevelopmentRepo/AOVIAS
            HEAD abc111
            branch refs/heads/main

            worktree V:/DevelopmentRepo/AOVIAS.Cursor
            HEAD def222
            branch refs/heads/agent/cursor/active

            worktree V:/DevelopmentRepo/AOVIAS.Claude
            HEAD ghi333
            branch refs/heads/agent/claude/active

            """;

        var worktrees = GitWorkspaceService.ParsePorcelainWorktrees(output);

        Assert.Equal(3, worktrees.Count);
        Assert.Equal(@"V:/DevelopmentRepo/AOVIAS", worktrees[0].Path);
        Assert.Equal("main", worktrees[0].Branch);
        Assert.Equal("abc111", worktrees[0].Head);
        Assert.False(worktrees[0].IsDetached);

        Assert.Equal(@"V:/DevelopmentRepo/AOVIAS.Cursor", worktrees[1].Path);
        Assert.Equal("agent/cursor/active", worktrees[1].Branch);

        Assert.Equal(@"V:/DevelopmentRepo/AOVIAS.Claude", worktrees[2].Path);
        Assert.Equal("agent/claude/active", worktrees[2].Branch);
    }

    [Fact]
    public void ParsePorcelainWorktrees_MarksDetachedHead()
    {
        const string output =
            """
            worktree C:/tmp/detached
            HEAD deadbeef
            detached

            """;

        var worktrees = GitWorkspaceService.ParsePorcelainWorktrees(output);
        Assert.Single(worktrees);
        Assert.True(worktrees[0].IsDetached);
        Assert.Null(worktrees[0].Branch);
    }
}
