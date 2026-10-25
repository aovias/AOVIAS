namespace ComplianceServiceManager.ViewModels.Deployment;

/// <summary>
/// Live pipeline stage for the Deployment Command Center.
/// </summary>
public sealed class DeploymentPipelineStageItem
{
    public required string Name { get; init; }

    public required string StageKey { get; init; }

    public string Status { get; set; } = "Waiting";

    public int ProgressPercent { get; set; }

    public string DurationDisplay { get; set; } = "--:--";

    // Deployment Workspace refactor: one consistent status glyph vocabulary across the whole
    // deployment surface — ○ waiting, ◐ running, ✔ completed, ⚠ warning, ✖ failed, ↻ retrying.
    public string StatusGlyph => Status switch
    {
        "Passed" or "Completed" => "✔",
        "Running" => "◐",
        "Failed" => "✖",
        "Warning" => "⚠",
        "Retrying" => "↻",
        _ => "○"
    };
}
