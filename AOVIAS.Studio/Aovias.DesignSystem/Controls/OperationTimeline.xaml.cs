using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace Aovias.DesignSystem.Controls;

/// <summary>
/// The live per-stage timeline for an Operation Engine run (✓ Completed / ⟳ Running / Pending /
/// Warning / Failed / Cancelled / Skipped), each stage expandable to its own structured log lines.
///
/// Generic by duck typing rather than a shared model type (this assembly never references
/// logic/domain projects — see the project file): each item in <see cref="Stages"/> is expected to
/// expose these properties, resolved by name at binding time —
/// <c>Name</c> (string), <c>DisplayDetail</c> (string — the stage's current task while running, or
/// its static description otherwise; the item, not this control, decides which), <c>StatusText</c>
/// (string — one of "Pending", "Running", "Completed", "Warning", "Failed", "Cancelled", "Skipped"),
/// <c>DurationDisplay</c> (string, may be empty), <c>FailureMessage</c> (string, may be empty),
/// <c>Logs</c> (an <c>IEnumerable&lt;string&gt;</c> of pre-formatted lines).
/// </summary>
public partial class OperationTimeline : UserControl
{
    public static readonly DependencyProperty StagesProperty =
        DependencyProperty.Register(nameof(Stages), typeof(IEnumerable), typeof(OperationTimeline), new PropertyMetadata(null));

    public OperationTimeline()
    {
        InitializeComponent();
    }

    public IEnumerable Stages
    {
        get => (IEnumerable)GetValue(StagesProperty);
        set => SetValue(StagesProperty, value);
    }
}
