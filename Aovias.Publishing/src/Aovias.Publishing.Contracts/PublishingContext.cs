namespace Aovias.Publishing.Contracts;

/// <summary>
/// Mutable context passed through the publishing pipeline.
/// </summary>
public sealed class PublishingContext
{
    public required PublishingRequest Request { get; init; }

    public ProjectDiscoveryResult? Discovery { get; set; }

    public DeploymentMetadata? DeploymentMetadata { get; set; }

    public string? WorkingDirectory { get; set; }

    public string? PublishOutputDirectory { get; set; }

    public string? DockerBuildContext { get; set; }

    public string? DockerfilePath { get; set; }

    public string? DockerImageName { get; set; }

    public string? DockerImageTag { get; set; }

    public string? DockerImageTarPath { get; set; }

    public string? RuntimeAgentBuildRevision { get; set; }

    public PublishingBuildIdentity? BuildIdentity { get; set; }

    public string? StagingDirectory { get; set; }

    public DeployManifest? Manifest { get; set; }

    public DeployHealthDefinition? Health { get; set; }

    public DeployPackageMetadata? Metadata { get; set; }

    public Dictionary<string, string> ConfigurationTemplate { get; } = new(StringComparer.OrdinalIgnoreCase);

    public string? ConfigurationTemplateJson { get; set; }

    public string? EnvTemplateContent { get; set; }

    public string? InterimPackagePath { get; set; }

    public string? ComposeContent { get; set; }

    public string? PackagePath { get; set; }

    public List<string> GeneratedFiles { get; } = [];

    public List<string> Warnings { get; } = [];

    public List<string> BuildOutput { get; } = [];

    public List<PublishingStageTiming> StageTimings { get; } = [];

    public DeployPackageVerificationResult? PackageVerification { get; set; }

    public DeploymentReadinessReport? ReadinessReport { get; set; }

    public Aovias.Deployment.Contracts.ServiceDeploymentBundle? DeploymentContracts { get; set; }

    public PublishingSession? Session { get; set; }

    public PublishingPipelineState? PipelineState { get; set; }

    public PackageVerificationReport? PackageVerificationReport { get; set; }

    public PublishingReliabilityReport? ReliabilityReport { get; set; }

    public PublishingExecutionOptions ExecutionOptions { get; set; } = new();

    public int TotalRetryCount { get; set; }

    public int RecoveryEventCount { get; set; }

    public IProgress<PublishingProgress>? Progress { get; init; }

    public void Report(string stepName, string status, string message, int percent)
    {
        Progress?.Report(new PublishingProgress
        {
            StepName = stepName,
            Status = status,
            Message = message,
            PercentComplete = percent
        });
    }
}
