namespace ComplianceServiceManager.ViewModels.Deployment;

/// <summary>
/// Certification checklist row (Sprint 12 D11).
/// </summary>
public sealed class CertificationPanelItem
{
    public required string Label { get; init; }

    public string Status { get; set; } = "Pending";

    public string Detail { get; set; } = string.Empty;

    public bool IsExpanded { get; set; }

    public string StatusGlyph => Status switch
    {
        "PASS" or "Passed" => "✔",
        "FAIL" or "Failed" => "✘",
        _ => "○"
    };
}
