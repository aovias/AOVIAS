using System.Windows.Controls;
using ComplianceServiceManager.ViewModels.Praman;

namespace ComplianceServiceManager.Views.Praman;

/// <summary>
/// PRAMAN enterprise application workspace hosted on AOVIAS Platform.
/// </summary>
public partial class PramanShellView : UserControl
{
    public PramanShellView()
    {
        InitializeComponent();
    }

    private async void PramanShellView_OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is PramanShellViewModel viewModel)
        {
            await viewModel.InitializeCommand.ExecuteAsync(null);
        }
    }
}
