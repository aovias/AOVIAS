using System.Windows.Controls;
using System.Windows.Input;
using ComplianceServiceManager.ViewModels.Overview;

namespace ComplianceServiceManager.Views.Overview;

/// <summary>
/// Overview workspace view.
/// </summary>
public partial class OverviewView : UserControl
{
    public OverviewView()
    {
        InitializeComponent();
    }

    private async void OverviewView_OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is OverviewViewModel viewModel)
        {
            await viewModel.InitializeCommand.ExecuteAsync(null);
        }
    }
}
