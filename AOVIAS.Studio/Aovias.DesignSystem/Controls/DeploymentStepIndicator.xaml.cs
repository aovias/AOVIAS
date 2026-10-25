using System.Windows;
using System.Windows.Controls;

namespace Aovias.DesignSystem.Controls;

/// <summary>
/// Horizontal deployment wizard step indicator.
/// </summary>
public partial class DeploymentStepIndicator : UserControl
{
    public static readonly DependencyProperty StepsProperty =
        DependencyProperty.Register(nameof(Steps), typeof(IEnumerable<string>), typeof(DeploymentStepIndicator), new PropertyMetadata(Array.Empty<string>()));

    public static readonly DependencyProperty CurrentStepIndexProperty =
        DependencyProperty.Register(nameof(CurrentStepIndex), typeof(int), typeof(DeploymentStepIndicator), new PropertyMetadata(-1));

    public DeploymentStepIndicator()
    {
        InitializeComponent();
    }

    public IEnumerable<string> Steps
    {
        get => (IEnumerable<string>)GetValue(StepsProperty);
        set => SetValue(StepsProperty, value);
    }

    public int CurrentStepIndex
    {
        get => (int)GetValue(CurrentStepIndexProperty);
        set => SetValue(CurrentStepIndexProperty, value);
    }
}
