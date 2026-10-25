using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ComplianceServiceManager.Models;
using ComplianceServiceManager.ViewModels;

namespace ComplianceServiceManager.Views.Compliance;

/// <summary>
/// Interaction logic for ComplianceDashboardView.xaml
/// </summary>
public partial class ComplianceDashboardView : UserControl
{
    public ComplianceDashboardView()
    {
        InitializeComponent();
    }

    private void StartAllButton_Click(object sender, RoutedEventArgs e)
    {
        ExecuteViewModelCommand(vm => vm.StartAllCommand);
        e.Handled = true;
    }

    private void StopAllButton_Click(object sender, RoutedEventArgs e)
    {
        ExecuteViewModelCommand(vm => vm.StopAllCommand);
        e.Handled = true;
    }

    private void RestartAllButton_Click(object sender, RoutedEventArgs e)
    {
        ExecuteViewModelCommand(vm => vm.RestartAllCommand);
        e.Handled = true;
    }

    private void RefreshServicesButton_Click(object sender, RoutedEventArgs e)
    {
        ExecuteViewModelCommand(vm => vm.RefreshServicesCommand);
        e.Handled = true;
    }

    private void StartServiceButton_Click(object sender, RoutedEventArgs e)
    {
        ExecuteViewModelCommand(vm => vm.StartServiceCommand, TryGetServiceParameter(sender));
        e.Handled = true;
    }

    private void StopServiceButton_Click(object sender, RoutedEventArgs e)
    {
        ExecuteViewModelCommand(vm => vm.StopServiceCommand, TryGetServiceParameter(sender));
        e.Handled = true;
    }

    private void RestartServiceButton_Click(object sender, RoutedEventArgs e)
    {
        ExecuteViewModelCommand(vm => vm.RestartServiceCommand, TryGetServiceParameter(sender));
        e.Handled = true;
    }

    private void OpenSwaggerButton_Click(object sender, RoutedEventArgs e)
    {
        ExecuteViewModelCommand(vm => vm.OpenSwaggerCommand, TryGetServiceParameter(sender));
        e.Handled = true;
    }

    private void AIFixServiceButton_Click(object sender, RoutedEventArgs e)
    {
        ExecuteViewModelCommand(vm => vm.AIFixServiceCommand, TryGetServiceParameter(sender));
        e.Handled = true;
    }

    private void FixFailedServicesButton_Click(object sender, RoutedEventArgs e)
    {
        ExecuteViewModelCommand(vm => vm.FixFailedServicesCommand);
        e.Handled = true;
    }

    private void RuntimeBuildLogsList_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.C || Keyboard.Modifiers != ModifierKeys.Control || DataContext is not MainViewModel viewModel)
        {
            return;
        }

        if (viewModel.CopyLogsCommand.CanExecute(RuntimeBuildLogsList.SelectedItems))
        {
            viewModel.CopyLogsCommand.Execute(RuntimeBuildLogsList.SelectedItems);
            e.Handled = true;
        }
    }

    private void ExecuteViewModelCommand(Func<MainViewModel, ICommand> commandSelector, object? parameter = null)
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        var command = commandSelector(viewModel);
        if (command.CanExecute(parameter))
        {
            command.Execute(parameter);
        }
    }

    private static ServiceInfoModel? TryGetServiceParameter(object sender)
    {
        return sender is FrameworkElement element ? element.DataContext as ServiceInfoModel : null;
    }
}
