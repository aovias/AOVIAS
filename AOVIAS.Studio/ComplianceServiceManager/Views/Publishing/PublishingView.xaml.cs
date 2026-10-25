using System.Windows.Controls;

namespace ComplianceServiceManager.Views.Publishing;

/// <summary>
/// Publishing workspace view.
/// </summary>
public partial class PublishingView : UserControl
{
    public PublishingView()
    {
        InitializeComponent();
    }

    private async void PublishingView_OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ViewModels.Publishing.PublishingViewModel viewModel)
        {
            await viewModel.InitializeCommand.ExecuteAsync(null);
        }
    }
}
