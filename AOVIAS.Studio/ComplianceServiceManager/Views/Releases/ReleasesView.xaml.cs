using System.Windows.Controls;
using ComplianceServiceManager.ViewModels.Releases;

namespace ComplianceServiceManager.Views.Releases;

/// <summary>
/// Releases workspace view.
/// </summary>
public partial class ReleasesView : UserControl
{
    public ReleasesView()
    {
        InitializeComponent();
    }

    private async void ReleasesView_OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ReleasesViewModel viewModel)
        {
            await viewModel.InitializeCommand.ExecuteAsync(null);
        }
    }
}
