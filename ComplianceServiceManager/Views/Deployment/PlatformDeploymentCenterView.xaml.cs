using System.Windows;
using System.Windows.Controls;
using ComplianceServiceManager.ViewModels.Deployment;

namespace ComplianceServiceManager.Views.Deployment;

public partial class PlatformDeploymentCenterView : UserControl
{
    public PlatformDeploymentCenterView()
    {
        InitializeComponent();
    }

    private async void PlatformDeploymentCenterView_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is PlatformDeploymentCenterViewModel viewModel)
        {
            await viewModel.InitializeCommand.ExecuteAsync(null);
        }
    }
}
