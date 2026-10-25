using System.Windows.Controls;

namespace ComplianceServiceManager.Views.ReleaseCenter;

/// <summary>
/// Release Center workspace view.
/// </summary>
public partial class ReleaseCenterView : UserControl
{
    public ReleaseCenterView()
    {
        InitializeComponent();
    }

    private async void ReleaseCenterView_OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is not ViewModels.ReleaseCenter.ReleaseCenterViewModel viewModel)
        {
            return;
        }

        try
        {
            await viewModel.InitializeCommand.ExecuteAsync(null);
        }
        catch (Exception exception)
        {
            viewModel.StatusMessage = $"Release Center failed to load: {exception.Message}";
        }
    }
}
