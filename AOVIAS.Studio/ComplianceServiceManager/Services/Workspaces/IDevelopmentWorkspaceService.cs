using ComplianceServiceManager.Models.Workspaces;

namespace ComplianceServiceManager.Services.Workspaces;

/// <summary>Combines Studio workspace configuration with read-only Git discovery.</summary>
public interface IDevelopmentWorkspaceService
{
    Task<IReadOnlyList<DevelopmentWorkspaceItem>> RefreshAsync(CancellationToken cancellationToken = default);
}
