namespace ComplianceServiceManager.ViewModels.Deployment;

/// <summary>
/// Pipeline step status for live deployment visualization.
/// </summary>
public sealed class DeploymentPipelineStepItem
{
    public required string Name { get; init; }

    public required string Status { get; set; }

    public string DisplayText => Status switch
    {
        "Completed" => $"✔ {Name}",
        "Running" => $"▶ {Name}",
        "Failed" => $"✘ {Name}",
        _ => $"○ {Name}"
    };
}
