using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using ComplianceServiceManager.ViewModels.Deployment;

namespace ComplianceServiceManager.Views.Deployment;

public partial class DeploymentCenterView : UserControl
{
    public DeploymentCenterView()
    {
        InitializeComponent();
    }

    private async void DeploymentCenterView_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is DeploymentCenterViewModel viewModel)
        {
            await viewModel.InitializeCommand.ExecuteAsync(null);
        }
    }

    private void DocumentationLink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }
}
