using Aovias.Publishing.Contracts;

namespace Aovias.Publishing.Core.Pipeline;

/// <summary>
/// Maps stage failures to deterministic categories.
/// </summary>
public static class PublishingFailureClassifier
{
    public static PublishingFailureCategory Classify(PublishingPipelineStageId stageId, string? message = null)
        => stageId switch
        {
            PublishingPipelineStageId.Discovery => PublishingFailureCategory.Build,
            PublishingPipelineStageId.Preflight => PublishingFailureCategory.Infrastructure,
            PublishingPipelineStageId.Restore => PublishingFailureCategory.ExternalDependency,
            PublishingPipelineStageId.Build => PublishingFailureCategory.Build,
            PublishingPipelineStageId.DockerBuild => ClassifyDocker(message),
            PublishingPipelineStageId.ContractGeneration => PublishingFailureCategory.Contracts,
            PublishingPipelineStageId.ConfigurationGeneration => PublishingFailureCategory.Configuration,
            PublishingPipelineStageId.ComposeGeneration => PublishingFailureCategory.Compose,
            PublishingPipelineStageId.PackageGeneration => PublishingFailureCategory.Package,
            PublishingPipelineStageId.PackageVerification => PublishingFailureCategory.Verification,
            PublishingPipelineStageId.DeploymentReadinessValidation => PublishingFailureCategory.Drv,
            PublishingPipelineStageId.ReleaseLibrary => PublishingFailureCategory.Infrastructure,
            PublishingPipelineStageId.DeploymentCertification => PublishingFailureCategory.Verification,
            _ => PublishingFailureCategory.Infrastructure
        };

    public static string Recommend(PublishingFailureCategory category, PublishingPipelineStageId stageId)
        => category switch
        {
            PublishingFailureCategory.Docker => "Verify Docker daemon availability and retry Docker Build.",
            PublishingFailureCategory.Verification => "Replay Package Verification after inspecting Verification.log.",
            PublishingFailureCategory.Drv => "Review DRV.log and replay Deployment Readiness Validation only.",
            PublishingFailureCategory.Contracts => "Replay Contract Generation and verify ServiceContractRegistry definitions.",
            PublishingFailureCategory.Package => "Replay Package Generation; prior Docker and Compose artifacts will be reused.",
            PublishingFailureCategory.Build => "Fix build errors in Build.log and replay from Build stage.",
            _ => $"Replay stage '{stageId}' after resolving {category} issues."
        };

    public static string RecoveryAction(PublishingFailureCategory category, PublishingPipelineStageId stageId)
        => category switch
        {
            PublishingFailureCategory.Docker => "Run stage: DockerBuild",
            PublishingFailureCategory.Verification => "Run stage: PackageVerification",
            PublishingFailureCategory.Drv => "Run stage: DeploymentReadinessValidation",
            PublishingFailureCategory.Package => "Run stage: PackageGeneration",
            _ => $"Run stage: {stageId}"
        };

    private static PublishingFailureCategory ClassifyDocker(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return PublishingFailureCategory.Docker;
        }

        if (message.Contains("daemon", StringComparison.OrdinalIgnoreCase)
            || message.Contains("Cannot connect", StringComparison.OrdinalIgnoreCase))
        {
            return PublishingFailureCategory.Infrastructure;
        }

        return PublishingFailureCategory.Docker;
    }
}
