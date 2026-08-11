namespace ComplianceServiceManager.Models.LocalPullRequests;

public sealed class LocalPullRequestRegistryDocument
{
    public List<LocalPullRequestDefinition> PullRequests { get; set; } = [];
}
