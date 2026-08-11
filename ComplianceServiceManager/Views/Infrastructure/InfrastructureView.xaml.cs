using System.Windows.Controls;
using ComplianceServiceManager.ViewModels.Infrastructure;

namespace ComplianceServiceManager.Views.Infrastructure;

/// <summary>
/// Infrastructure workspace view.
/// </summary>
public partial class InfrastructureView : UserControl
{
    public InfrastructureView()
    {
        InitializeComponent();
    }

    private async void InfrastructureView_OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is InfrastructureViewModel viewModel)
        {
            await viewModel.InitializeCommand.ExecuteAsync(null);
        }
    }
}
