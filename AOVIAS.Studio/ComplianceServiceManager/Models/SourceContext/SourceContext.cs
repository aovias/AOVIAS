using ComplianceServiceManager.Models.Workspaces;

namespace ComplianceServiceManager.Models.SourceContext;

/// <summary>
/// First-class source identity for Shared Runtime and workspace runtimes.
/// Branch and HEAD are observed from Git; never persisted as authoritative state.
/// </summary>
public sealed class SourceContext
{
    public required string ContextId { get; init; }

    public required SourceContextKind Kind { get; init; }

    public string? WorkspaceId { get; init; }

    public required string RootPath { get; init; }

    public string? Branch { get; init; }

    public string? Head { get; init; }

    public WorkspaceAgentOwner Agent { get; init; } = WorkspaceAgentOwner.Unknown;

    public string Ownership { get; init; } = string.Empty;

    public SourceContextGitValidity GitValidity { get; init; } = SourceContextGitValidity.Unavailable;

    public string? GitStatusReason { get; init; }

    public DateTimeOffset? ObservedAt { get; init; }

    public bool IsAccessible => GitValidity == SourceContextGitValidity.Accessible;

    public string DisplayLabel => Kind switch
    {
        SourceContextKind.Shared => "Shared Source",
        SourceContextKind.Workspace when string.Equals(WorkspaceId, SourceContextIds.Cursor, StringComparison.OrdinalIgnoreCase) => "Cursor Source",
        SourceContextKind.Workspace when string.Equals(WorkspaceId, SourceContextIds.Claude, StringComparison.OrdinalIgnoreCase) => "Claude Source",
        SourceContextKind.Workspace => $"{WorkspaceId ?? "Workspace"} Source",
        _ => ContextId
    };
}
