using System.Windows;
using ComplianceServiceManager.Services.PortRegistry;

namespace ComplianceServiceManager.Views.Development;

public partial class AssignPortWindow : Window
{
    private readonly Func<int, PortRegistryResult> validate;

    public AssignPortWindow(
        string serviceName,
        string instanceLabel,
        int? suggestedPort,
        Func<int, PortRegistryResult> validate)
    {
        InitializeComponent();
        this.validate = validate;
        ServiceText.Text = serviceName;
        InstanceText.Text = instanceLabel;
        PortBox.Text = suggestedPort?.ToString() ?? string.Empty;
        PortBox.TextChanged += (_, _) => RefreshValidation();
        RefreshValidation();
    }

    public int SelectedPort { get; private set; }

    private void RefreshValidation()
    {
        if (!int.TryParse(PortBox.Text.Trim(), out var port))
        {
            ValidationText.Text = "✗ Enter a numeric port.";
            AssignButton.IsEnabled = false;
            return;
        }

        var result = validate(port);
        if (!result.Succeeded)
        {
            ValidationText.Text = $"✗ {result.Error}";
            AssignButton.IsEnabled = false;
            return;
        }

        ValidationText.Text =
            "✓ Valid port\n✓ Instance exists\n✓ Workspace rules satisfied\n✓ Port not assigned elsewhere";
        AssignButton.IsEnabled = true;
        SelectedPort = port;
    }

    private void OnAssignClick(object sender, RoutedEventArgs e)
    {
        RefreshValidation();
        if (!AssignButton.IsEnabled)
        {
            MessageBox.Show(this, ValidationText.Text, "Assign Port", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
