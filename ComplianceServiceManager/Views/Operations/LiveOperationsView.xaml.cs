using System.Windows;
using System.Windows.Controls;
using ComplianceServiceManager.ViewModels.Operations;

namespace ComplianceServiceManager.Views.Operations;

/// <summary>
/// Live operations view powered by SignalR.
/// </summary>
public partial class LiveOperationsView : UserControl
{
    public LiveOperationsView()
    {
        InitializeComponent();
    }

    private async void LiveOperationsView_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is LiveOperationsViewModel viewModel)
        {
            await viewModel.ConnectCommand.ExecuteAsync(null);
        }
    }
}
