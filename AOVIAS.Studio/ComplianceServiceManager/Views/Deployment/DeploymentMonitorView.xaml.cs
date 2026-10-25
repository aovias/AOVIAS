using System.Windows.Controls;

namespace ComplianceServiceManager.Views.Deployment;

/// <summary>
/// Operations → Deployment Monitor (Deployment Workspace refactor — the former "Deployment Command
/// Center", renamed and reduced to observation: live sessions, pipeline, per-service progress,
/// phase timeline, console, history, and rollback. Shares the same singleton
/// <c>PlatformDeploymentCenterViewModel</c> the unified Deploy workflow drives, so whatever is
/// deploying is what this monitors — no deployment initiation exists here.
/// </summary>
public partial class DeploymentMonitorView : UserControl
{
    public DeploymentMonitorView()
    {
        InitializeComponent();
    }
}
