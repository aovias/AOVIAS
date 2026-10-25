namespace ComplianceServiceManager.ViewModels.Deployment;

/// <summary>
/// Deployment workspace mode (Sprint 12 D1).
/// </summary>
public enum DeploymentWorkspaceMode
{
    EntirePlatform,
    SingleService
}

/// <summary>
/// Pipeline stage definitions driven by deployment orchestration (Sprint 12 D4).
/// </summary>
public static class DeploymentPipelineCatalog
{
    public static readonly IReadOnlyList<string> PlatformStages =
    [
        "Package Analysis",
        "Upload",
        "Infrastructure",
        "Deployment",
        "Health",
        "Certification"
    ];

    public static readonly IReadOnlyList<string> ServiceWorkflowSteps =
    [
        "Validate Package",
        "Verify Runtime",
        "Verify Dependencies",
        "Backup Current Release",
        "Docker Compose Stop",
        "Release Extraction",
        "Stage Release",
        "Load Docker Image",
        "Ensure Runtime Folders",
        "Docker Compose Up",
        "Configure Nginx",
        "Health Verification",
        "Deployment Acceptance",
        "Activate Release",
        "Register Deployment",
        "Register Runtime",
        "Audit"
    ];

    public static readonly IReadOnlyList<string> PhaseTimelineSteps =
    [
        "Analyze",
        "Upload",
        "Provision",
        "Deploy",
        "Health",
        "Certified"
    ];

    public static readonly IReadOnlyList<string> PlatformServiceIds =
    [
        "runtime-agent",
        "gateway",
        "identity-service",
        "organization-service",
        "compliance-service",
        "audit-service",
        "ai",
        "portal"
    ];

    public static readonly IReadOnlyList<string> SingleServiceIds =
    [
        "gateway",
        "identity-service",
        "portal",
        "ai",
        "runtime-agent"
    ];

    public static readonly IReadOnlyList<string> CatalogFilters =
    [
        "All",
        "Latest",
        "Production",
        "Development",
        "Certified",
        "Needs Attention",
        "Selected",
        "Unselected"
    ];
}
