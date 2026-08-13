using ComplianceServiceManager.Models;
using ComplianceServiceManager.Models.LocalPullRequests;
using ComplianceServiceManager.Models.ResourceIntelligence;
using ComplianceServiceManager.Models.ServiceInstances;
using ComplianceServiceManager.Models.Workspaces;

namespace ComplianceServiceManager.Tests.Presentation;

public sealed class OperatorLabelsTests
{
    [Theory]
    [InlineData("cursor", "Cursor")]
    [InlineData("claude", "Claude")]
    [InlineData("main", "Shared")]
    [InlineData(null, "Shared")]
    public void Workspace_UsesCanonicalOperatorNames(string? workspaceId, string expected)
        => Assert.Equal(expected, OperatorLabels.Workspace(workspaceId));

    [Fact]
    public void Owner_UsesSharedRuntimeAndWorkspaceLabels()
    {
        Assert.Equal("Shared Runtime", OperatorLabels.Owner(ServiceInstanceOwnership.Shared, null));
        Assert.Equal("Cursor Workspace", OperatorLabels.Owner(ServiceInstanceOwnership.Workspace, "cursor"));
        Assert.Equal("Claude Workspace", OperatorLabels.Owner(ServiceInstanceOwnership.Workspace, "claude"));
    }

    [Fact]
    public void Pressure_And_ResourceLevel_AreTitleCase()
    {
        Assert.Equal("Normal", OperatorLabels.Pressure(ResourcePressureLevel.Normal));
        Assert.Equal("High", OperatorLabels.Pressure(ResourcePressureLevel.High));
        Assert.Equal("Critical", OperatorLabels.Pressure(ResourcePressureLevel.Critical));
        Assert.Equal("Low", OperatorLabels.ResourceLevel(RuntimeProfileResourceClass.Light));
        Assert.Equal("Moderate", OperatorLabels.ResourceLevel(RuntimeProfileResourceClass.Medium));
        Assert.Equal("High", OperatorLabels.ResourceLevel(RuntimeProfileResourceClass.Heavy));
    }

    [Fact]
    public void ShortSha_TruncatesWithoutInventing()
    {
        Assert.Equal("—", OperatorLabels.ShortSha(null));
        Assert.Equal("abc", OperatorLabels.ShortSha("abc"));
        Assert.Equal("1234567890ab", OperatorLabels.ShortSha("1234567890abcdef"));
    }

    [Fact]
    public void ApplyConfigurationDisplays_NormalizesPersistedAllCapsNames()
    {
        var item = new DevelopmentWorkspaceItem
        {
            Id = "cursor",
            DisplayName = "CURSOR",
            AgentOwner = WorkspaceAgentOwner.Cursor
        };
        item.ApplyConfigurationDisplays();
        Assert.Equal("Cursor", item.DisplayName);
        Assert.Equal("Cursor", item.AgentOwnerDisplay);
    }

    [Fact]
    public void LocalPullRequest_ExposesLabeledSourceAndTargetShas()
    {
        var item = new LocalPullRequestItem();
        item.ApplyComparison(new LocalPullRequestComparison
        {
            SourceTipSha = "aaaaaaaaaaaabbbb",
            TargetTipSha = "ccccccccccccdddd",
            SourceClean = true,
            TargetClean = true,
            Ahead = 1,
            Behind = 0
        });
        Assert.Equal("aaaaaaaaaaaa", item.SourceShaDisplay);
        Assert.Equal("cccccccccccc", item.TargetShaDisplay);
    }

    [Fact]
    public void ServiceInstance_Formatters_MatchCanonicalVocabulary()
    {
        Assert.Equal("Shared", ServiceInstanceItem.FormatWorkspaceLabel("main"));
        Assert.Equal("Cursor", ServiceInstanceItem.FormatWorkspaceLabel("cursor"));
        Assert.Equal("Shared Runtime", ServiceInstanceItem.FormatOwnershipDisplay(ServiceInstanceOwnership.Shared, null));
        Assert.Equal("Cursor Workspace", ServiceInstanceItem.FormatOwnershipDisplay(ServiceInstanceOwnership.Workspace, "cursor"));
    }
}
