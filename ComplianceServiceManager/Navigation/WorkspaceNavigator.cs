namespace ComplianceServiceManager.Navigation;

/// <summary>
/// Event-driven workspace navigator used by overview quick actions.
/// </summary>
public sealed class WorkspaceNavigator : IWorkspaceNavigator
{
    /// <summary>
    /// Raised when a workspace navigation request is issued.
    /// </summary>
    public event Action<string>? NavigationRequested;

    /// <inheritdoc />
    public void NavigateTo(string workspace) => NavigationRequested?.Invoke(workspace);
}
