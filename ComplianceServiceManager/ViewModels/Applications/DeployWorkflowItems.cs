using CommunityToolkit.Mvvm.ComponentModel;

namespace ComplianceServiceManager.ViewModels.Applications;

/// <summary>
/// One step in the unified Deploy workflow's always-visible stepper
/// (1 Runtime → 2 Release → 3 Services → 4 Validation → 5 Deploy → 6 Verification).
/// </summary>
public sealed partial class DeployWorkflowStepItem : ObservableObject
{
    public required int Number { get; init; }

    public required string Title { get; init; }

    /// <summary>The x:Name of the section this step scrolls to when clicked — resolved by the view's code-behind.</summary>
    public required string SectionName { get; init; }

    [ObservableProperty]
    private bool isComplete;

    [ObservableProperty]
    private bool isActive;
}

/// <summary>
/// One selectable service in the unified Deploy workflow's Service Selection section.
/// Selecting exactly one IS single-service deployment; selecting several IS multi-service
/// deployment — same screen, same engine.
/// </summary>
public sealed partial class DeployServiceSelectionItem : ObservableObject
{
    public required string ServiceId { get; init; }

    public required string DisplayName { get; init; }

    [ObservableProperty]
    private bool isSelected;

    public event Action? SelectionChanged;

    partial void OnIsSelectedChanged(bool value) => SelectionChanged?.Invoke();
}

/// <summary>
/// One deployment blocker — Problem / Reason / Recovery / Technical Details / Fix Command.
/// Never a bare HTTP status.
/// </summary>
public sealed record DeployBlockerItem
{
    public required string Problem { get; init; }

    public required string Reason { get; init; }

    public required string Recovery { get; init; }

    public string? TechnicalDetails { get; init; }

    public string? FixCommand { get; init; }

    public string Category { get; init; } = "Validation";
}

/// <summary>
/// One row of the Deployment Plan checklist — every validation visible; failures expandable.
/// </summary>
public sealed record DeployPlanCheckItem
{
    public required string Label { get; init; }

    public required string Status { get; init; }

    public string Detail { get; init; } = string.Empty;

    public string Impact { get; init; } = string.Empty;

    public string Recovery { get; init; } = string.Empty;

    public string StatusGlyph => Status switch
    {
        "Passed" or "PASS" or "Ready" or "Healthy" or "Connected" or "Available" => "✔",
        "Failed" or "FAIL" or "Unavailable" or "Error" => "✖",
        "Warning" => "⚠",
        "Running" => "◐",
        _ => "○"
    };

    public bool IsFailure => StatusGlyph == "✖";

    public bool IsExpandable => IsFailure || !string.IsNullOrWhiteSpace(Detail);
}
