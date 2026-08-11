using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using ComplianceServiceManager.Services.Bootstrap;
using ComplianceServiceManager.ViewModels.Platform;

namespace ComplianceServiceManager.Views.Platform;

/// <summary>
/// Platform → Bootstrap workspace view.
/// </summary>
public partial class BootstrapView : UserControl
{
    public BootstrapView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is BootstrapViewModel viewModel && sender is PasswordBox passwordBox)
        {
            viewModel.Password = passwordBox.Password;
        }
    }

    private void SudoPasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is BootstrapViewModel viewModel && sender is PasswordBox passwordBox)
        {
            viewModel.SudoElevationPrompt.Password = passwordBox.Password;
        }
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is BootstrapViewModel oldViewModel)
        {
            oldViewModel.SudoElevationPrompt.PropertyChanged -= OnSudoElevationPromptPropertyChanged;
        }

        if (e.NewValue is BootstrapViewModel newViewModel)
        {
            newViewModel.SudoElevationPrompt.PropertyChanged += OnSudoElevationPromptPropertyChanged;
        }
    }

    /// <summary>WPF's PasswordBox cannot be data-bound (by design, so a password never lingers in the
    /// visual tree's data layer) — this is the one place that clears the box's own visible dots each
    /// time a fresh prompt appears or the previous one is submitted/cancelled, so stale characters
    /// are never left showing.</summary>
    private void OnSudoElevationPromptPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Deliberately only IsVisible, not Password — Password also changes on every keystroke (via
        // SudoPasswordBox_OnPasswordChanged), and clearing the box then would fight the operator's
        // own typing.
        if (e.PropertyName == nameof(SudoElevationPromptViewModel.IsVisible))
        {
            SudoPasswordInput.Clear();
        }
    }
}
