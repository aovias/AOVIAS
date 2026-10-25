namespace ComplianceServiceManager.Navigation;

/// <summary>
/// Presentation-layer navigation contract for workspace switching.
/// </summary>
public interface IWorkspaceNavigator
{
    /// <summary>
    /// Requests navigation to the named workspace.
    /// </summary>
    /// <param name="workspace">The workspace identifier.</param>
    void NavigateTo(string workspace);
}
