using System.Windows;
using System.Windows.Controls;
using ComplianceServiceManager.ViewModels.Observability;

namespace ComplianceServiceManager.Views.Observability;

/// <summary>
/// Interaction logic for ObservabilityView.xaml.
/// </summary>
public partial class ObservabilityView : UserControl
{
    public ObservabilityView()
    {
        InitializeComponent();
    }

    private async void ObservabilityView_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ObservabilityViewModel viewModel)
        {
            await viewModel.InitializeCommand.ExecuteAsync(null);
        }
    }
}
