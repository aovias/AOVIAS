using System.Windows;
using ComplianceServiceManager.ViewModels;

namespace ComplianceServiceManager;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    /// <param name="shellViewModel">The shell view model coordinating workspace navigation.</param>
    public MainWindow(ShellViewModel shellViewModel)
    {
        InitializeComponent();
        DataContext = shellViewModel;
    }
}
