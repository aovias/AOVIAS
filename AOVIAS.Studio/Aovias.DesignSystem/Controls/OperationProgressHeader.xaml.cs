using System.Windows;
using System.Windows.Controls;

namespace Aovias.DesignSystem.Controls;

/// <summary>
/// Overall-progress header for any long-running Operation Engine run: progress bar + percentage,
/// current stage/task, elapsed and estimated-remaining time. Generic — takes only primitive values,
/// no dependency on any specific operation kind or the Operation Engine's own types (this assembly
/// never references logic/domain projects; see the project file).
/// </summary>
public partial class OperationProgressHeader : UserControl
{
    public static readonly DependencyProperty OverallProgressPercentProperty =
        DependencyProperty.Register(nameof(OverallProgressPercent), typeof(int), typeof(OperationProgressHeader), new PropertyMetadata(0));

    public static readonly DependencyProperty StatusTextProperty =
        DependencyProperty.Register(nameof(StatusText), typeof(string), typeof(OperationProgressHeader), new PropertyMetadata("Running"));

    public static readonly DependencyProperty CurrentStageNameProperty =
        DependencyProperty.Register(nameof(CurrentStageName), typeof(string), typeof(OperationProgressHeader), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty CurrentTaskProperty =
        DependencyProperty.Register(nameof(CurrentTask), typeof(string), typeof(OperationProgressHeader), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ElapsedDisplayProperty =
        DependencyProperty.Register(nameof(ElapsedDisplay), typeof(string), typeof(OperationProgressHeader), new PropertyMetadata("0:00"));

    public static readonly DependencyProperty EstimatedRemainingDisplayProperty =
        DependencyProperty.Register(nameof(EstimatedRemainingDisplay), typeof(string), typeof(OperationProgressHeader), new PropertyMetadata(string.Empty));

    public OperationProgressHeader()
    {
        InitializeComponent();
    }

    public int OverallProgressPercent
    {
        get => (int)GetValue(OverallProgressPercentProperty);
        set => SetValue(OverallProgressPercentProperty, value);
    }

    public string StatusText
    {
        get => (string)GetValue(StatusTextProperty);
        set => SetValue(StatusTextProperty, value);
    }

    public string CurrentStageName
    {
        get => (string)GetValue(CurrentStageNameProperty);
        set => SetValue(CurrentStageNameProperty, value);
    }

    public string CurrentTask
    {
        get => (string)GetValue(CurrentTaskProperty);
        set => SetValue(CurrentTaskProperty, value);
    }

    public string ElapsedDisplay
    {
        get => (string)GetValue(ElapsedDisplayProperty);
        set => SetValue(ElapsedDisplayProperty, value);
    }

    /// <summary>Empty string when no estimate is available yet — see <c>OperationInstance.EstimatedRemaining</c>'s "null until real progress exists" rule. The XAML hides the "Est. remaining" row entirely when this is empty, rather than showing a fabricated placeholder.</summary>
    public string EstimatedRemainingDisplay
    {
        get => (string)GetValue(EstimatedRemainingDisplayProperty);
        set => SetValue(EstimatedRemainingDisplayProperty, value);
    }
}
